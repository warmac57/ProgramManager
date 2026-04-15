Imports System.Runtime.InteropServices
Imports System.Xml

Public Class Form1

    ' --- Shell icon extraction for folders ---
    <DllImport("shell32.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function SHGetFileInfo(pszPath As String, dwFileAttributes As UInteger,
                                          ByRef psfi As SHFILEINFO, cbSizeFileInfo As UInteger,
                                          uFlags As UInteger) As IntPtr
    End Function

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
    Private Structure SHFILEINFO
        Public hIcon As IntPtr
        Public iIcon As Integer
        Public dwAttributes As UInteger
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=260)>
        Public szDisplayName As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=80)>
        Public szTypeName As String
    End Structure

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function DestroyIcon(hIcon As IntPtr) As Boolean
    End Function

    Private Const SHGFI_ICON As UInteger = &H100
    Private Const SHGFI_LARGEICON As UInteger = &H0

    ' Path to the layout file stored next to the executable.
    Private ReadOnly LayoutFilePath As String =
        IO.Path.Combine(Application.StartupPath, "ProgramManagerLayout.xml")

    ' --- Form state variables ---
    Private formOriginalSize As Size
    Private formOriginalLocation As Point
    Private isRolledUp As Boolean = False
    Private Const RollUpHeight As Integer = 35 ' Height of title bar when rolled up

    ' --- Drag state: delay DoDragDrop until the mouse actually moves ---
    Private dragStartPoint As Point = Point.Empty
    Private dragStartIcon As PictureBox = Nothing
    Private Const DragThreshold As Integer = 6

    ' --- Right-click context menu ---
    Private WithEvents IconContextMenu As New ContextMenuStrip()
    Private mnuOpenFolder As New ToolStripMenuItem("Open Containing Folder")
    Private mnuEditNote As New ToolStripMenuItem("Edit Note...")
    Private mnuToggleHidden As New ToolStripMenuItem("Hide Folder")
    Private mnuRemove As New ToolStripMenuItem("Remove")
    Private contextTarget As PictureBox = Nothing

    ' --- Notes: maps each PictureBox icon to its user note ---
    Private iconNotes As New Dictionary(Of PictureBox, String)()

    ' --- Missing links: tracks icons whose file/folder path no longer exists ---
    Private missingLinks As New HashSet(Of PictureBox)()

    ' --- Tab name textbox references (mapped from designer controls) ---
    Private tabNameTextBoxes() As TextBox

    ' --- Grid panel references (mapped from designer controls) ---
    Private gridPanels() As TableLayoutPanel

    ' --- Theme state ---
    Private isDarkMode As Boolean = False

    ' Dark theme colors
    Private ReadOnly DarkFormBack As Color = Color.FromArgb(30, 30, 30)
    Private ReadOnly DarkTabBack As Color = Color.FromArgb(40, 40, 40)
    Private ReadOnly DarkCellBack As Color = Color.FromArgb(50, 50, 55)
    Private ReadOnly DarkForeColor As Color = Color.FromArgb(220, 220, 220)
    Private ReadOnly DarkControlBack As Color = Color.FromArgb(55, 55, 60)

    ' Light theme colors
    Private ReadOnly LightFormBack As Color = SystemColors.Control
    Private ReadOnly LightTabBack As Color = SystemColors.Window
    Private ReadOnly LightCellBack As Color = Color.WhiteSmoke
    Private ReadOnly LightForeColor As Color = SystemColors.ControlText
    Private ReadOnly LightControlBack As Color = SystemColors.Window

    ' --- Form Load / Close ---

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Store original form size and location
        formOriginalSize = Me.Size
        formOriginalLocation = Me.Location

        ' Load form settings (size, position, and title)
        LoadFormSettings()

        ' Build the right-click context menu.
        AddHandler mnuOpenFolder.Click, AddressOf MnuOpenFolder_Click
        AddHandler mnuEditNote.Click, AddressOf MnuEditNote_Click
        AddHandler mnuToggleHidden.Click, AddressOf MnuToggleHidden_Click
        AddHandler mnuRemove.Click, AddressOf MnuRemove_Click

        IconContextMenu.Items.Add(mnuOpenFolder)
        IconContextMenu.Items.Add(mnuEditNote)
        IconContextMenu.Items.Add(mnuToggleHidden)
        IconContextMenu.Items.Add(New ToolStripSeparator())
        IconContextMenu.Items.Add(mnuRemove)

        ' Map designer controls into arrays for easy indexed access.
        gridPanels = {GridPanel1, GridPanel2, GridPanel3, GridPanel4, GridPanel5, GridPanel6, GridPanel7, GridPanel8, GridPanel9}
        tabNameTextBoxes = {txtTabName1, txtTabName2, txtTabName3, txtTabName4, txtTabName5, txtTabName6, txtTabName7, txtTabName8, txtTabName9}

        ' Populate cell panels inside each grid.
        For Each tlp As TableLayoutPanel In gridPanels
            SetupTableCells(tlp)
        Next

        ' Wire up tab name textboxes to update tab headers live.
        For i As Integer = 0 To 8
            tabNameTextBoxes(i).Tag = i
            AddHandler tabNameTextBoxes(i).TextChanged, AddressOf TabNameTextBox_TextChanged
        Next

        ' Wire up Tab 5 buttons.
        AddHandler btnApply.Click, AddressOf ApplyTabNames_Click
        AddHandler btnDarkModeToggle.Click, AddressOf DarkModeToggle_Click
        AddHandler lvwAllLinks.DoubleClick, AddressOf AllLinksListView_DoubleClick

        ' Wire up tab drawing and tab change.
        AddHandler TabControl1.DrawItem, AddressOf TabControl1_DrawItem
        AddHandler TabControl1.SelectedIndexChanged, AddressOf TabControl1_SelectedIndexChanged

        BackupXmlFiles()
        LoadLayout()
    End Sub

    Private Sub TabControl1_DrawItem(sender As Object, e As DrawItemEventArgs)
        Dim tc As TabControl = CType(sender, TabControl)
        Dim bounds As Rectangle = tc.GetTabRect(e.Index)
        Dim tabText As String = tc.TabPages(e.Index).Text
        Dim isSelected As Boolean = (tc.SelectedIndex = e.Index)

        ' Pick colors based on theme and selection state.
        Dim backColor As Color
        Dim foreColor As Color
        Dim borderColor As Color

        If isDarkMode Then
            backColor = If(isSelected, DarkTabBack, DarkFormBack)
            foreColor = DarkForeColor
            borderColor = Color.FromArgb(100, 100, 100)
        Else
            backColor = If(isSelected, Color.White, Color.FromArgb(230, 230, 230))
            foreColor = SystemColors.ControlText
            borderColor = Color.FromArgb(160, 160, 160)
        End If

        ' Fill tab background.
        Using brush As New SolidBrush(backColor)
            e.Graphics.FillRectangle(brush, bounds)
        End Using

        ' Draw border around the tab header.
        Using pen As New Pen(borderColor, 1)
            e.Graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1)
        End Using

        ' Draw tab text centered.
        TextRenderer.DrawText(e.Graphics, tabText, tc.Font, bounds, foreColor,
                              TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        SaveLayout()
        SaveFormSettings()
    End Sub

    ' --- Table Setup ---

    ''' <summary>
    ''' Creates a Panel inside every TableLayoutPanel cell.
    ''' Each panel acts as a drop-target for icons and shows a subtle border.
    ''' </summary>
    Private Sub SetupTableCells(tlp As TableLayoutPanel)
        For row As Integer = 0 To tlp.RowCount - 1
            For col As Integer = 0 To tlp.ColumnCount - 1
                ' Skip cells that already have a control (designer safety).
                If tlp.GetControlFromPosition(col, row) IsNot Nothing Then Continue For

                Dim cell As New Panel() With {
                    .Dock = DockStyle.Fill,
                    .Margin = New Padding(2),
                    .AllowDrop = True,
                    .BorderStyle = BorderStyle.FixedSingle,
                    .BackColor = Color.WhiteSmoke
                }

                ' Wire up drag-drop on the cell panel.
                AddHandler cell.DragEnter, AddressOf Cell_DragEnter
                AddHandler cell.DragDrop, AddressOf Cell_DragDrop

                ' Assign context menu so right-click works on empty cells too.
                cell.ContextMenuStrip = IconContextMenu

                tlp.Controls.Add(cell, col, row)
            Next
        Next
    End Sub

    ' --- Tab 5 event handlers ---

    Private Sub TabNameTextBox_TextChanged(sender As Object, e As EventArgs)
        Dim txt As TextBox = CType(sender, TextBox)
        Dim tabIdx As Integer = CInt(txt.Tag)
        Dim newName As String = txt.Text.Trim()
        If Not String.IsNullOrEmpty(newName) Then
            TabControl1.TabPages(tabIdx).Text = newName
        End If
    End Sub

    Private Sub ApplyTabNames_Click(sender As Object, e As EventArgs)
        SaveLayout()
    End Sub

    Private Sub DarkModeToggle_Click(sender As Object, e As EventArgs)
        isDarkMode = Not isDarkMode
        ApplyTheme()
        SaveLayout()
    End Sub

    Private Sub ApplyTheme()
        If isDarkMode Then
            btnDarkModeToggle.Text = "Switch to Light Mode"
            Me.BackColor = DarkFormBack
            TabControl1.BackColor = DarkFormBack

            ' Style tabs 1-9 (icon grid tabs)
            For i As Integer = 0 To 8
                Dim tabPage As TabPage = TabControl1.TabPages(i)
                tabPage.BackColor = DarkTabBack
                tabPage.ForeColor = DarkForeColor

                Dim tlp As TableLayoutPanel = gridPanels(i)
                tlp.BackColor = DarkTabBack

                For Each ctrl As Control In tlp.Controls
                    If TypeOf ctrl Is Panel Then
                        Dim cell As Panel = CType(ctrl, Panel)
                        Dim hasHiddenIcon As Boolean = False
                        For Each child As Control In cell.Controls
                            If TypeOf child Is PictureBox AndAlso child.BackColor = Color.FromArgb(80, Color.Gray) Then
                                hasHiddenIcon = True
                            End If
                            If TypeOf child Is Label Then
                                Dim lbl As Label = CType(child, Label)
                                If lbl.ForeColor <> Color.Gray Then
                                    lbl.ForeColor = DarkForeColor
                                End If
                            End If
                        Next
                        If Not hasHiddenIcon Then
                            cell.BackColor = DarkCellBack
                        End If
                    End If
                Next
            Next

            ' Style All Links tab
            TabPage9.BackColor = DarkTabBack
            TabPage9.ForeColor = DarkForeColor
            StyleTab5Controls(DarkTabBack, DarkControlBack, DarkForeColor)
        Else
            btnDarkModeToggle.Text = "Switch to Dark Mode"
            Me.BackColor = LightFormBack
            TabControl1.BackColor = LightFormBack

            For i As Integer = 0 To 8
                Dim tabPage As TabPage = TabControl1.TabPages(i)
                tabPage.BackColor = SystemColors.Window
                tabPage.ForeColor = LightForeColor
                tabPage.UseVisualStyleBackColor = True

                Dim tlp As TableLayoutPanel = gridPanels(i)
                tlp.BackColor = SystemColors.Window

                For Each ctrl As Control In tlp.Controls
                    If TypeOf ctrl Is Panel Then
                        Dim cell As Panel = CType(ctrl, Panel)
                        Dim hasHiddenIcon As Boolean = False
                        For Each child As Control In cell.Controls
                            If TypeOf child Is PictureBox AndAlso child.BackColor = Color.FromArgb(80, Color.Gray) Then
                                hasHiddenIcon = True
                            End If
                            If TypeOf child Is Label Then
                                Dim lbl As Label = CType(child, Label)
                                If lbl.ForeColor <> Color.Gray Then
                                    lbl.ForeColor = LightForeColor
                                End If
                            End If
                        Next
                        If Not hasHiddenIcon Then
                            cell.BackColor = LightCellBack
                        End If
                    End If
                Next
            Next

            TabPage9.BackColor = SystemColors.Window
            TabPage9.ForeColor = LightForeColor
            TabPage9.UseVisualStyleBackColor = True
            StyleTab5Controls(SystemColors.Window, LightControlBack, LightForeColor)
        End If
    End Sub

    Private Sub StyleTab5Controls(backColor As Color, controlBack As Color, foreColor As Color)
        For Each ctrl As Control In TabPage9.Controls
            StyleControlRecursive(ctrl, backColor, controlBack, foreColor)
        Next
    End Sub

    Private Sub StyleControlRecursive(ctrl As Control, backColor As Color, controlBack As Color, foreColor As Color)
        ctrl.ForeColor = foreColor

        If TypeOf ctrl Is TextBox Then
            ctrl.BackColor = controlBack
        ElseIf TypeOf ctrl Is Button Then
            ctrl.BackColor = controlBack
        ElseIf TypeOf ctrl Is ListView Then
            ctrl.BackColor = controlBack
            CType(ctrl, ListView).ForeColor = foreColor
        ElseIf TypeOf ctrl Is Panel OrElse TypeOf ctrl Is TableLayoutPanel Then
            ctrl.BackColor = backColor
        End If

        For Each child As Control In ctrl.Controls
            StyleControlRecursive(child, backColor, controlBack, foreColor)
        Next
    End Sub

    Private Sub TabControl1_SelectedIndexChanged(sender As Object, e As EventArgs)
        If TabControl1.SelectedIndex = 9 Then
            RefreshAllLinksList()
            ' Also refresh the tab name textboxes to reflect current names.
            For i As Integer = 0 To 8
                tabNameTextBoxes(i).Text = TabControl1.TabPages(i).Text
            Next
        End If
    End Sub

    Private Sub RefreshAllLinksList()
        lvwAllLinks.Items.Clear()
        For tabIndex As Integer = 0 To 8
            Dim tlp As TableLayoutPanel = gridPanels(tabIndex)
            Dim tabName As String = TabControl1.TabPages(tabIndex).Text
            For row As Integer = 0 To tlp.RowCount - 1
                For col As Integer = 0 To tlp.ColumnCount - 1
                    Dim ctrl As Control = tlp.GetControlFromPosition(col, row)
                    If TypeOf ctrl Is Panel AndAlso ctrl.Controls.Count > 0 Then
                        For Each child As Control In ctrl.Controls
                            If TypeOf child Is PictureBox AndAlso child.Tag IsNot Nothing Then
                                Dim pb As PictureBox = CType(child, PictureBox)
                                Dim filePath As String = pb.Tag.ToString()
                                Dim noteText As String = ""
                                iconNotes.TryGetValue(pb, noteText)
                                Dim displayName As String
                                If IO.Directory.Exists(filePath) Then
                                    displayName = IO.Path.GetFileName(filePath)
                                Else
                                    displayName = IO.Path.GetFileNameWithoutExtension(filePath)
                                End If
                                Dim item As New ListViewItem(tabName)
                                item.SubItems.Add(displayName)
                                item.SubItems.Add(filePath)
                                item.SubItems.Add(noteText)
                                item.Tag = filePath
                                lvwAllLinks.Items.Add(item)
                            End If
                        Next
                    End If
                Next
            Next
        Next
    End Sub

    Private Sub AllLinksListView_DoubleClick(sender As Object, e As EventArgs)
        If lvwAllLinks.SelectedItems.Count = 0 Then Return
        Dim filePath As String = lvwAllLinks.SelectedItems(0).Tag?.ToString()
        If String.IsNullOrEmpty(filePath) Then Return
        Try
            Dim startInfo As New ProcessStartInfo(filePath) With {
                .UseShellExecute = True,
                .WorkingDirectory = IO.Path.GetDirectoryName(filePath)
            }
            Process.Start(startInfo)
        Catch ex As Exception
            MessageBox.Show("Could not launch the file." & vbCrLf & vbCrLf &
                            "Path: " & filePath & vbCrLf & vbCrLf &
                            "System Error: " & ex.Message,
                            "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' --- Helper: get the active tab's TableLayoutPanel (only for tabs 0-3) ---

    Private Function GetActiveTableLayoutPanel() As TableLayoutPanel
        Dim idx As Integer = TabControl1.SelectedIndex
        If idx >= 0 AndAlso idx <= 8 Then
            Return gridPanels(idx)
        End If
        Return Nothing
    End Function

    ' --- External File Drop (onto the form / table) ---

    Private Sub Form1_DragEnter(sender As Object, e As DragEventArgs) Handles Me.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
        Else
            e.Effect = DragDropEffects.None
        End If
    End Sub

    Private Sub Form1_DragDrop(sender As Object, e As DragEventArgs) Handles Me.DragDrop
        ' Only handle external file drops here; internal moves are handled by Cell_DragDrop.
        If Not e.Data.GetDataPresent(DataFormats.FileDrop) Then Return

        Dim tlp As TableLayoutPanel = GetActiveTableLayoutPanel()
        If tlp Is Nothing Then Return

        Dim files As String() = CType(e.Data.GetData(DataFormats.FileDrop), String())

        For Each filePath As String In files
            Dim cell As Panel = FindFirstEmptyCell(tlp)
            If cell Is Nothing Then
                MessageBox.Show("All cells are full. Remove an icon first.",
                                "Grid Full", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            PlaceIconInCell(cell, filePath)
        Next
    End Sub

    ' --- Cell Drag-Enter / Drop (for internal moves AND external drops) ---

    Private Sub Cell_DragEnter(sender As Object, e As DragEventArgs)
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            ' External file drop.
            e.Effect = DragDropEffects.Copy
        ElseIf e.Data.GetDataPresent(GetType(Panel)) Then
            ' Internal icon move (we pass the source cell Panel).
            e.Effect = DragDropEffects.Move
        Else
            e.Effect = DragDropEffects.None
        End If
    End Sub

    Private Sub Cell_DragDrop(sender As Object, e As DragEventArgs)
        Dim targetCell As Panel = CType(sender, Panel)

        If e.Data.GetDataPresent(GetType(Panel)) Then
            ' --- Internal move: swap ALL controls between source and target cells ---
            Dim sourceCell As Panel = CType(e.Data.GetData(GetType(Panel)), Panel)

            If targetCell Is sourceCell Then Return

            ' Collect controls from both cells before modifying.
            Dim sourceControls As New List(Of Control)
            For Each c As Control In sourceCell.Controls
                sourceControls.Add(c)
            Next
            Dim targetControls As New List(Of Control)
            For Each c As Control In targetCell.Controls
                targetControls.Add(c)
            Next

            ' Remove all from both.
            sourceCell.Controls.Clear()
            targetCell.Controls.Clear()

            ' Swap: source controls go to target, target controls go to source.
            For Each c As Control In sourceControls
                targetCell.Controls.Add(c)
            Next
            For Each c As Control In targetControls
                sourceCell.Controls.Add(c)
            Next

        ElseIf e.Data.GetDataPresent(DataFormats.FileDrop) Then
            ' --- External file drop directly onto a cell ---
            Dim files As String() = CType(e.Data.GetData(DataFormats.FileDrop), String())
            If targetCell.Controls.Count > 0 Then
                MessageBox.Show("This cell already has an icon. Drop onto an empty cell or the form background.",
                                "Cell Occupied", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            ' Place only the first file in this cell; remaining go to empty cells.
            Dim tlp As TableLayoutPanel = GetActiveTableLayoutPanel()
            For Each filePath As String In files
                If targetCell.Controls.Count = 0 Then
                    PlaceIconInCell(targetCell, filePath)
                Else
                    If tlp IsNot Nothing Then
                        Dim emptyCell As Panel = FindFirstEmptyCell(tlp)
                        If emptyCell IsNot Nothing Then
                            PlaceIconInCell(emptyCell, filePath)
                        End If
                    End If
                End If
            Next
        End If
    End Sub

    ' --- Icon Creation ---

    ''' <summary>
    ''' Creates a PictureBox with the file's associated icon and a label,
    ''' and places it inside the given cell panel.
    ''' </summary>
    Private Sub PlaceIconInCell(cell As Panel, filePath As String, Optional note As String = "")
        Try
            Dim isDirectory As Boolean = IO.Directory.Exists(filePath)
            Dim isMissing As Boolean = Not isDirectory AndAlso Not IO.File.Exists(filePath)
            Dim iconBitmap As Bitmap

            If isMissing Then
                ' File or folder no longer exists — use a generic warning icon.
                iconBitmap = SystemIcons.Warning.ToBitmap()
            ElseIf isDirectory Then
                ' Use SHGetFileInfo to get the folder icon.
                Dim shfi As New SHFILEINFO()
                SHGetFileInfo(filePath, 0, shfi, CUInt(Marshal.SizeOf(shfi)),
                              SHGFI_ICON Or SHGFI_LARGEICON)
                If shfi.hIcon <> IntPtr.Zero Then
                    iconBitmap = Icon.FromHandle(shfi.hIcon).ToBitmap()
                    DestroyIcon(shfi.hIcon)
                Else
                    iconBitmap = SystemIcons.WinLogo.ToBitmap()
                End If
            Else
                ' Extract the file's associated icon.
                Dim fileIcon As Icon = Icon.ExtractAssociatedIcon(filePath)
                iconBitmap = fileIcon.ToBitmap()
            End If

            Dim iconBox As New PictureBox() With {
                .Image = iconBitmap,
                .SizeMode = PictureBoxSizeMode.Zoom,
                .Tag = filePath,
                .Cursor = Cursors.Hand,
                .BackColor = If(isMissing, Color.FromArgb(80, Color.Gray), Color.Transparent),
                .ContextMenuStrip = IconContextMenu
            }

            ' Wire up events.
            AddHandler iconBox.DoubleClick, AddressOf IconBox_DoubleClick
            AddHandler iconBox.MouseDown, AddressOf IconBox_MouseDown
            AddHandler iconBox.MouseMove, AddressOf IconBox_MouseMove
            AddHandler iconBox.MouseUp, AddressOf IconBox_MouseUp
            AddHandler iconBox.Paint, AddressOf IconBox_Paint

            ' Add a label below the icon showing the name.
            ' GetFileNameWithoutExtension works correctly even for missing paths and folders.
            Dim displayName As String
            If isDirectory Then
                displayName = IO.Path.GetFileName(filePath)
            Else
                displayName = IO.Path.GetFileNameWithoutExtension(filePath)
                If String.IsNullOrEmpty(displayName) Then displayName = IO.Path.GetFileName(filePath)
            End If

            Dim nameLabel As New Label() With {
                .Text = displayName,
                .AutoSize = False,
                .TextAlign = ContentAlignment.TopCenter,
                .Dock = DockStyle.Bottom,
                .Height = 30,
                .Font = New Font("Segoe UI", 8),
                .BackColor = Color.Transparent,
                .ForeColor = If(isMissing, Color.Gray, SystemColors.ControlText)
            }

            ' Layout: icon centered above the label.
            iconBox.Dock = DockStyle.Fill

            cell.Controls.Add(nameLabel)
            cell.Controls.Add(iconBox)
            iconBox.BringToFront()

            ' Track missing links so double-click shows a helpful message instead of crashing.
            If isMissing Then
                missingLinks.Add(iconBox)
            End If

            ' Register the note if one was provided.
            If Not String.IsNullOrEmpty(note) Then
                iconNotes(iconBox) = note
            End If

            ' Apply hidden indicator only for existing desktop folders.
            If isDirectory AndAlso Not isMissing Then
                UpdateHiddenIndicator(iconBox)
            End If

        Catch ex As Exception
            MessageBox.Show("Could not create an icon for: " & filePath & vbCrLf &
                            "Error: " & ex.Message, "Icon Creation Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    ' --- Icon Mouse-Down / Move / Up: deferred drag so double-click works ---

    Private Sub IconBox_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            ' Just record the start point; don't begin the drag yet.
            dragStartPoint = e.Location
            dragStartIcon = CType(sender, PictureBox)
        End If
    End Sub

    Private Sub IconBox_MouseMove(sender As Object, e As MouseEventArgs)
        ' Only start a drag if the mouse has moved far enough from the click point.
        If dragStartIcon IsNot Nothing AndAlso e.Button = MouseButtons.Left Then
            Dim dx As Integer = Math.Abs(e.X - dragStartPoint.X)
            Dim dy As Integer = Math.Abs(e.Y - dragStartPoint.Y)
            If dx > DragThreshold OrElse dy > DragThreshold Then
                ' Pass the parent cell panel so the drop handler can move ALL its children.
                Dim sourceCell As Panel = CType(dragStartIcon.Parent, Panel)
                dragStartIcon = Nothing
                dragStartPoint = Point.Empty
                sourceCell.DoDragDrop(sourceCell, DragDropEffects.Move)
            End If
        End If
    End Sub

    Private Sub IconBox_MouseUp(sender As Object, e As MouseEventArgs)
        ' Reset drag tracking if the user released without moving far enough.
        dragStartIcon = Nothing
        dragStartPoint = Point.Empty
    End Sub

    ' --- Note indicator badge painted on the icon ---

    Private Sub IconBox_Paint(sender As Object, e As PaintEventArgs)
        Dim pb As PictureBox = CType(sender, PictureBox)
        Dim hasNote As Boolean = iconNotes.ContainsKey(pb) AndAlso Not String.IsNullOrEmpty(iconNotes(pb))
        If Not hasNote Then Return

        Const BadgeSize As Integer = 20
        Const Margin As Integer = 2
        Dim x As Integer = pb.Width - BadgeSize - Margin
        Dim y As Integer = Margin

        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

        Using bgBrush As New SolidBrush(Color.FromArgb(220, 40, 167, 69))
            e.Graphics.FillEllipse(bgBrush, x, y, BadgeSize, BadgeSize)
        End Using

        Using fgBrush As New SolidBrush(Color.White)
            Using badgeFont As New Font("Segoe UI", 10F, FontStyle.Bold)
                Dim sf As New StringFormat() With {
                    .Alignment = StringAlignment.Center,
                    .LineAlignment = StringAlignment.Center
                }
                e.Graphics.DrawString("✓", badgeFont, fgBrush,
                                      New RectangleF(x, y, BadgeSize, BadgeSize), sf)
            End Using
        End Using
    End Sub

    ' --- Icon Double-Click: launch the file ---

    Private Sub IconBox_DoubleClick(sender As Object, e As EventArgs)
        Dim clickedIcon As PictureBox = CType(sender, PictureBox)
        If clickedIcon.Tag Is Nothing Then Return

        Dim filePath As String = clickedIcon.Tag.ToString()

        ' If the link is known-missing, inform the user rather than trying to launch.
        If missingLinks.Contains(clickedIcon) Then
            MessageBox.Show("This shortcut points to a file or folder that no longer exists." & vbCrLf & vbCrLf &
                            "Path: " & filePath & vbCrLf & vbCrLf &
                            "Right-click the icon and choose Remove to delete it from the grid.",
                            "Missing Link", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            Dim startInfo As New ProcessStartInfo(filePath) With {
                .UseShellExecute = True,
                .WorkingDirectory = IO.Path.GetDirectoryName(filePath)
            }
            Process.Start(startInfo)
        Catch ex As Exception
            MessageBox.Show("Could not launch the file." & vbCrLf & vbCrLf &
                            "Path: " & filePath & vbCrLf & vbCrLf &
                            "System Error: " & ex.Message,
                            "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' --- Title bar double-click to roll up/unroll ---
    Private Const WM_NCLBUTTONDBLCLK As Integer = &HA3
    Private Const HTCAPTION As Integer = 2

    Protected Overrides Sub WndProc(ByRef m As Message)
        If m.Msg = WM_NCLBUTTONDBLCLK AndAlso CInt(m.WParam) = HTCAPTION Then
            ToggleRollUp()
            Return
        End If
        MyBase.WndProc(m)
    End Sub

    Private Sub ToggleRollUp()
        If isRolledUp Then
            Me.MinimumSize = Size.Empty
            Me.Size = formOriginalSize
            Me.MinimumSize = New Size(400, 300)
            isRolledUp = False
        Else
            formOriginalSize = Me.Size
            Me.MinimumSize = Size.Empty
            Me.Size = New Size(Me.Width, RollUpHeight)
            isRolledUp = True
        End If
    End Sub

    ' --- XML Backup ---

    ''' <summary>
    ''' Copies the layout and settings XML files into the BACKUP-XML subfolder,
    ''' stamped with the current date/time. Keeps only the 10 most recent backups
    ''' per file; older ones are deleted.
    ''' </summary>
    Private Sub BackupXmlFiles()
        Try
            Dim backupDir As String = IO.Path.Combine(Application.StartupPath, "BACKUP-XML")
            IO.Directory.CreateDirectory(backupDir)

            Dim timestamp As String = DateTime.Now.ToString("yyyyMMdd_HHmmss")

            Dim filesToBackup() As String = {
                IO.Path.Combine(Application.StartupPath, "ProgramManagerLayout.xml"),
                IO.Path.Combine(Application.StartupPath, "ProgramManagerSettings.xml")
            }

            For Each srcFile As String In filesToBackup
                If Not IO.File.Exists(srcFile) Then Continue For

                Dim baseName As String = IO.Path.GetFileNameWithoutExtension(srcFile)
                Dim destPath As String = IO.Path.Combine(backupDir, $"{baseName}_{timestamp}.xml")
                IO.File.Copy(srcFile, destPath, True)

                ' Prune: keep only the 10 most recent backups for this file.
                Dim existing As String() = IO.Directory.GetFiles(backupDir, baseName & "_*.xml")
                Array.Sort(existing)   ' Alphabetical = chronological given the timestamp format.
                Dim excess As Integer = existing.Length - 10
                If excess > 0 Then
                    For i As Integer = 0 To excess - 1
                        IO.File.Delete(existing(i))
                    Next
                End If
            Next
        Catch
            ' Backup failure is non-critical; continue silently.
        End Try
    End Sub

    ' --- Form Settings Save/Load ---

    Private Sub SaveFormSettings()
        Try
            Dim settingsFile As String = IO.Path.Combine(Application.StartupPath, "ProgramManagerSettings.xml")
            Dim doc As New XmlDocument()
            Dim root As XmlElement = doc.CreateElement("Settings")
            doc.AppendChild(root)

            Dim saveSize As Size = If(isRolledUp, formOriginalSize, Me.Size)

            Dim windowElement As XmlElement = doc.CreateElement("Window")
            windowElement.SetAttribute("left", Me.Left.ToString())
            windowElement.SetAttribute("top", Me.Top.ToString())
            windowElement.SetAttribute("width", saveSize.Width.ToString())
            windowElement.SetAttribute("height", saveSize.Height.ToString())
            windowElement.SetAttribute("maximized", Me.WindowState.ToString())
            root.AppendChild(windowElement)

            doc.Save(settingsFile)

        Catch ex As Exception
            ' Silently fail for settings - not critical
        End Try
    End Sub

    Private Sub LoadFormSettings()
        Dim settingsFile As String = IO.Path.Combine(Application.StartupPath, "ProgramManagerSettings.xml")
        If Not IO.File.Exists(settingsFile) Then Return

        Try
            Dim doc As New XmlDocument()
            doc.Load(settingsFile)

            Dim windowNode As XmlNode = doc.SelectSingleNode("//Window")
            If windowNode IsNot Nothing Then
                Dim left As Integer = Integer.Parse(windowNode.Attributes("left").Value)
                Dim top As Integer = Integer.Parse(windowNode.Attributes("top").Value)
                Dim width As Integer = Integer.Parse(windowNode.Attributes("width").Value)
                Dim height As Integer = Integer.Parse(windowNode.Attributes("height").Value)

                Dim workingArea As Rectangle = Screen.PrimaryScreen.WorkingArea

                If left < workingArea.Left Then left = workingArea.Left
                If top < workingArea.Top Then top = workingArea.Top
                If left + width > workingArea.Right Then left = workingArea.Right - width
                If top + height > workingArea.Bottom Then top = workingArea.Bottom - height

                Dim maximized As String = windowNode.Attributes("maximized").Value
                Dim wasMaximized As Boolean = (maximized = FormWindowState.Maximized.ToString())

                ' If the form was saved maximized, reset to default size and center on screen
                If wasMaximized Then
                    Me.WindowState = FormWindowState.Normal
                    Me.Size = New Size(825, 800)
                    Me.StartPosition = FormStartPosition.CenterScreen
                Else
                    Me.StartPosition = FormStartPosition.Manual
                    Me.Location = New Point(left, top)
                    Me.Size = New Size(width, height)
                End If
            End If

        Catch ex As Exception
            ' If settings fail to load, use defaults
        End Try
    End Sub

    ' --- Right-click context menu handlers ---

    Private Sub MnuOpenFolder_Click(sender As Object, e As EventArgs)
        If contextTarget Is Nothing OrElse contextTarget.Tag Is Nothing Then Return
        Dim filePath As String = contextTarget.Tag.ToString()
        Try
            Process.Start("explorer.exe", "/select, """ & filePath & """")
        Catch ex As Exception
            MessageBox.Show("Could not open folder." & vbCrLf & ex.Message,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub MnuRemove_Click(sender As Object, e As EventArgs)
        If contextTarget Is Nothing Then Return
        Dim cell As Panel = CType(contextTarget.Parent, Panel)
        iconNotes.Remove(contextTarget)
        missingLinks.Remove(contextTarget)
        cell.Controls.Clear()
        contextTarget = Nothing
    End Sub

    Private Sub MnuEditNote_Click(sender As Object, e As EventArgs)
        If contextTarget Is Nothing OrElse contextTarget.Tag Is Nothing Then Return
        Dim filePath As String = contextTarget.Tag.ToString()
        Dim currentNote As String = ""
        If iconNotes.ContainsKey(contextTarget) Then
            currentNote = iconNotes(contextTarget)
        End If

        Dim newNote As String = ShowNoteDialog(filePath, currentNote)
        If newNote Is Nothing Then Return  ' cancelled

        If String.IsNullOrEmpty(newNote) Then
            iconNotes.Remove(contextTarget)
        Else
            iconNotes(contextTarget) = newNote
        End If
        contextTarget?.Invalidate()
        SaveLayout()
    End Sub

    ''' <summary>
    ''' Shows a simple dialog for entering or editing the note for an icon.
    ''' Returns the new note text, or Nothing if cancelled.
    ''' </summary>
    Private Function ShowNoteDialog(filePath As String, currentNote As String) As String
        Dim displayName As String
        If IO.Directory.Exists(filePath) Then
            displayName = IO.Path.GetFileName(filePath)
        Else
            displayName = IO.Path.GetFileNameWithoutExtension(filePath)
        End If

        Dim result As String = Nothing

        Using dlg As New Form()
            dlg.Text = "Edit Note"
            dlg.Size = New Size(450, 270)
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog
            dlg.StartPosition = FormStartPosition.CenterParent
            dlg.MaximizeBox = False
            dlg.MinimizeBox = False

            Dim lbl As New Label() With {
                .Text = "Note for: " & displayName,
                .AutoSize = True,
                .Location = New Point(12, 12)
            }

            Dim txt As New TextBox() With {
                .Multiline = True,
                .ScrollBars = ScrollBars.Vertical,
                .Text = currentNote,
                .Location = New Point(12, 38),
                .Size = New Size(382, 110),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
            }

            Dim btnOK As New Button() With {
                .Text = "OK",
                .DialogResult = DialogResult.OK,
                .Size = New Size(80, 30),
                .Location = New Point(234, 158)
            }

            Dim btnCancel As New Button() With {
                .Text = "Cancel",
                .DialogResult = DialogResult.Cancel,
                .Size = New Size(80, 30),
                .Location = New Point(320, 158)
            }

            dlg.Controls.AddRange({lbl, txt, btnOK, btnCancel})
            dlg.AcceptButton = btnOK
            dlg.CancelButton = btnCancel

            If isDarkMode Then
                dlg.BackColor = DarkFormBack
                lbl.ForeColor = DarkForeColor
                txt.BackColor = DarkControlBack
                txt.ForeColor = DarkForeColor
                btnOK.BackColor = DarkControlBack
                btnOK.ForeColor = DarkForeColor
                btnCancel.BackColor = DarkControlBack
                btnCancel.ForeColor = DarkForeColor
            End If

            If dlg.ShowDialog(Me) = DialogResult.OK Then
                result = txt.Text
            End If
        End Using

        Return result
    End Function

    Private Sub MnuToggleHidden_Click(sender As Object, e As EventArgs)
        If contextTarget Is Nothing OrElse contextTarget.Tag Is Nothing Then Return
        Dim folderPath As String = contextTarget.Tag.ToString()
        If Not IO.Directory.Exists(folderPath) Then Return

        Try
            Dim attrs As IO.FileAttributes = IO.File.GetAttributes(folderPath)
            If (attrs And IO.FileAttributes.Hidden) = IO.FileAttributes.Hidden Then
                IO.File.SetAttributes(folderPath, attrs And Not IO.FileAttributes.Hidden)
            Else
                IO.File.SetAttributes(folderPath, attrs Or IO.FileAttributes.Hidden)
            End If
            UpdateHiddenIndicator(contextTarget)
        Catch ex As Exception
            MessageBox.Show("Could not change hidden attribute." & vbCrLf & ex.Message,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function IsOnDesktop(path As String) As Boolean
        Dim parent As String = IO.Path.GetDirectoryName(path)
        If parent Is Nothing Then Return False
        Dim desktop As String = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        Dim desktopPublic As String = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
        Return String.Equals(parent, desktop, StringComparison.OrdinalIgnoreCase) OrElse
               String.Equals(parent, desktopPublic, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Sub UpdateHiddenIndicator(iconBox As PictureBox)
        If iconBox Is Nothing OrElse iconBox.Tag Is Nothing Then Return
        Dim folderPath As String = iconBox.Tag.ToString()
        If Not IO.Directory.Exists(folderPath) Then Return

        Dim isHidden As Boolean = False
        Try
            Dim attrs As IO.FileAttributes = IO.File.GetAttributes(folderPath)
            isHidden = (attrs And IO.FileAttributes.Hidden) = IO.FileAttributes.Hidden
        Catch
            Return
        End Try

        Dim cell As Panel = TryCast(iconBox.Parent, Panel)
        Dim nameLabel As Label = Nothing
        If cell IsNot Nothing Then
            For Each ctrl As Control In cell.Controls
                If TypeOf ctrl Is Label Then
                    nameLabel = CType(ctrl, Label)
                    Exit For
                End If
            Next
        End If

        If isHidden Then
            iconBox.BackColor = Color.FromArgb(80, Color.Gray)
            If nameLabel IsNot Nothing Then
                nameLabel.ForeColor = Color.Gray
                Dim baseName As String = IO.Path.GetFileName(folderPath)
                If Not nameLabel.Text.StartsWith("(H) ") Then
                    nameLabel.Text = "(H) " & baseName
                End If
            End If
        Else
            iconBox.BackColor = Color.Transparent
            If nameLabel IsNot Nothing Then
                nameLabel.ForeColor = Control.DefaultForeColor
                Dim baseName As String = IO.Path.GetFileName(folderPath)
                nameLabel.Text = baseName
            End If
        End If
    End Sub

    Private Sub IconContextMenu_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles IconContextMenu.Opening
        Dim cms As ContextMenuStrip = CType(sender, ContextMenuStrip)
        contextTarget = TryCast(cms.SourceControl, PictureBox)

        If contextTarget Is Nothing Then
            e.Cancel = True
            Return
        End If

        Dim filePath As String = contextTarget.Tag?.ToString()
        If filePath IsNot Nothing AndAlso IO.Directory.Exists(filePath) AndAlso IsOnDesktop(filePath) Then
            mnuToggleHidden.Visible = True
            Try
                Dim attrs As IO.FileAttributes = IO.File.GetAttributes(filePath)
                If (attrs And IO.FileAttributes.Hidden) = IO.FileAttributes.Hidden Then
                    mnuToggleHidden.Text = "Unhide Folder"
                Else
                    mnuToggleHidden.Text = "Hide Folder"
                End If
            Catch
                mnuToggleHidden.Visible = False
            End Try
        Else
            mnuToggleHidden.Visible = False
        End If
    End Sub

    ' --- Helper: find the first cell panel with no icon ---

    Private Function FindFirstEmptyCell(tlp As TableLayoutPanel) As Panel
        For row As Integer = 0 To tlp.RowCount - 1
            For col As Integer = 0 To tlp.ColumnCount - 1
                Dim ctrl As Control = tlp.GetControlFromPosition(col, row)
                If TypeOf ctrl Is Panel AndAlso ctrl.Controls.Count = 0 Then
                    Return CType(ctrl, Panel)
                End If
            Next
        Next
        Return Nothing
    End Function

    ' --- Save Layout to XML ---

    Private Sub SaveLayout()
        Try
            Dim doc As New XmlDocument()
            Dim root As XmlElement = doc.CreateElement("ProgramManagerLayout")
            doc.AppendChild(root)

            ' Save theme preference.
            Dim settingsElement As XmlElement = doc.CreateElement("Settings")
            settingsElement.SetAttribute("darkMode", isDarkMode.ToString())
            root.AppendChild(settingsElement)

            Dim tabsElement As XmlElement = doc.CreateElement("Tabs")
            root.AppendChild(tabsElement)

            For tabIndex As Integer = 0 To 8
                Dim tlp As TableLayoutPanel = gridPanels(tabIndex)

                Dim tabElement As XmlElement = doc.CreateElement("Tab")
                tabElement.SetAttribute("index", tabIndex.ToString())
                tabElement.SetAttribute("name", TabControl1.TabPages(tabIndex).Text)
                tabsElement.AppendChild(tabElement)

                For row As Integer = 0 To tlp.RowCount - 1
                    For col As Integer = 0 To tlp.ColumnCount - 1
                        Dim ctrl As Control = tlp.GetControlFromPosition(col, row)
                        If TypeOf ctrl Is Panel AndAlso ctrl.Controls.Count > 0 Then
                            For Each child As Control In ctrl.Controls
                                If TypeOf child Is PictureBox AndAlso child.Tag IsNot Nothing Then
                                    Dim pb As PictureBox = CType(child, PictureBox)
                                    Dim iconEl As XmlElement = doc.CreateElement("Icon")
                                    iconEl.SetAttribute("column", col.ToString())
                                    iconEl.SetAttribute("row", row.ToString())
                                    iconEl.SetAttribute("filePath", pb.Tag.ToString())
                                    Dim savedNote As String = ""
                                    If iconNotes.TryGetValue(pb, savedNote) AndAlso Not String.IsNullOrEmpty(savedNote) Then
                                        iconEl.SetAttribute("note", savedNote)
                                    End If
                                    tabElement.AppendChild(iconEl)
                                End If
                            Next
                        End If
                    Next
                Next
            Next

            doc.Save(LayoutFilePath)

        Catch ex As Exception
            MessageBox.Show("Could not save layout: " & ex.Message,
                            "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    ' --- Load Layout from XML ---

    Private Sub LoadLayout()
        If Not IO.File.Exists(LayoutFilePath) Then Return

        Try
            Dim doc As New XmlDocument()
            doc.Load(LayoutFilePath)

            ' Load theme preference.
            Dim settingsNode As XmlNode = doc.SelectSingleNode("//Settings")
            If settingsNode IsNot Nothing Then
                Dim darkAttr As XmlAttribute = settingsNode.Attributes("darkMode")
                If darkAttr IsNot Nothing Then
                    isDarkMode = Boolean.Parse(darkAttr.Value)
                    ApplyTheme()
                End If
            End If

            ' Try new tabbed format first.
            Dim tabNodes As XmlNodeList = doc.SelectNodes("//Tabs/Tab")
            If tabNodes IsNot Nothing AndAlso tabNodes.Count > 0 Then
                For Each tabNode As XmlNode In tabNodes
                    Dim tabIndex As Integer = Integer.Parse(tabNode.Attributes("index").Value)
                    If tabIndex < 0 OrElse tabIndex > 8 Then Continue For

                    ' Restore tab name.
                    Dim tabName As String = tabNode.Attributes("name")?.Value
                    If Not String.IsNullOrEmpty(tabName) Then
                        TabControl1.TabPages(tabIndex).Text = tabName
                    End If

                    Dim tlp As TableLayoutPanel = gridPanels(tabIndex)

                    Dim iconNodes As XmlNodeList = tabNode.SelectNodes("Icon")
                    If iconNodes Is Nothing Then Continue For

                    For Each node As XmlNode In iconNodes
                        Dim col As Integer = Integer.Parse(node.Attributes("column").Value)
                        Dim row As Integer = Integer.Parse(node.Attributes("row").Value)
                        Dim filePath As String = node.Attributes("filePath").Value
                        Dim note As String = If(node.Attributes("note")?.Value, "")

                        If col < 0 OrElse col >= tlp.ColumnCount Then Continue For
                        If row < 0 OrElse row >= tlp.RowCount Then Continue For

                        Dim cell As Control = tlp.GetControlFromPosition(col, row)
                        If TypeOf cell Is Panel AndAlso cell.Controls.Count = 0 Then
                            PlaceIconInCell(CType(cell, Panel), filePath, note)
                        End If
                    Next
                Next
            Else
                ' Fall back to legacy single-grid format (load into tab 0).
                Dim iconNodes As XmlNodeList = doc.SelectNodes("//Icons/Icon")
                If iconNodes Is Nothing Then Return

                Dim tlp As TableLayoutPanel = gridPanels(0)

                For Each node As XmlNode In iconNodes
                    Dim col As Integer = Integer.Parse(node.Attributes("column").Value)
                    Dim row As Integer = Integer.Parse(node.Attributes("row").Value)
                    Dim filePath As String = node.Attributes("filePath").Value

                    If col < 0 OrElse col >= tlp.ColumnCount Then Continue For
                    If row < 0 OrElse row >= tlp.RowCount Then Continue For

                    Dim cell As Control = tlp.GetControlFromPosition(col, row)
                    If TypeOf cell Is Panel AndAlso cell.Controls.Count = 0 Then
                        PlaceIconInCell(CType(cell, Panel), filePath)
                    End If
                Next
            End If

        Catch ex As Exception
            MessageBox.Show("Could not load layout: " & ex.Message,
                            "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

End Class
