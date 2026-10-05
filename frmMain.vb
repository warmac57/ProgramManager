Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.Xml

Public Class frmMain

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

    ' --- DWM title bar caption color (Windows 11) for the save-confirmation flash ---
    <DllImport("dwmapi.dll", PreserveSig:=True)>
    Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer,
                                                  ByRef attrValue As Integer, attrSize As Integer) As Integer
    End Function

    Private Const DWMWA_CAPTION_COLOR As Integer = 35
    Private Const DWMWA_COLOR_DEFAULT As Integer = -1   ' restores the system default caption color

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

    ' --- Right-click menu on the tab headers (add / rename / move / delete tabs) ---
    Private TabHeaderMenu As New ContextMenuStrip()
    Private mnuTabAdd As New ToolStripMenuItem("Add Tab...")
    Private mnuTabRename As New ToolStripMenuItem("Rename...")
    Private mnuTabMoveLeft As New ToolStripMenuItem("Move Left")
    Private mnuTabMoveRight As New ToolStripMenuItem("Move Right")
    Private mnuTabDelete As New ToolStripMenuItem("Delete Tab...")
    Private tabMenuTarget As TabPage = Nothing   ' grid tab that was right-clicked; Nothing for All Links

    ' --- Notes: maps each PictureBox icon to its user note ---
    Private iconNotes As New Dictionary(Of PictureBox, String)()

    ' --- Custom names: maps a PictureBox icon to the name shown instead of its file name ---
    Private iconNames As New Dictionary(Of PictureBox, String)()

    ' --- Link editor (bottom of the All Links tab) ---
    Private editingIcon As PictureBox = Nothing          ' link loaded into the editor, or Nothing
    Private editorTabPages As New List(Of TabPage)()     ' tab behind each entry of cboLinkTab
    Private loadingEditor As Boolean = False             ' True while fields are filled in by code
    Private listSelectionPending As Boolean = False      ' a row change is queued (see AllLinksList_SelectedIndexChanged)
    Private tabChangeDepth As Integer = 0                ' >0 while code adds, moves or removes tab pages

    ' --- Missing links: tracks icons whose file/folder path no longer exists ---
    Private missingLinks As New HashSet(Of PictureBox)()

    ' --- Grid tabs are built at runtime (see AddGridTab), one per <Tab> in the layout XML. ---
    ' Each grid tab stores its TableLayoutPanel in TabPage.Tag; All Links always stays last.
    Private Const GridColumns As Integer = 4
    Private Const GridRows As Integer = 4

    ' Tabs created when there is no layout file yet (matches the original fixed tab set).
    Private ReadOnly DefaultTabNames() As String =
        {"Tab 1", "Tab 2", "Tab 3", "Tab 4", "Tab 5", "Tab 6", "Tab 7", "Tab 8", "eMails", "Tab 10"}

    ' Set when the layout file exists but could not be read. Saving is then skipped
    ' for the session so a failed load can never overwrite the user's real layout.
    Private layoutLoadFailed As Boolean = False

    ' True while ReloadLayout has torn down the grid tabs and is rebuilding them;
    ' a save in that window would write a half-empty layout, so SaveLayout skips it.
    Private isReloadingLayout As Boolean = False

    ' True when there was no layout file at startup, i.e. this copy has never been run.
    ' frmMain_Shown then does the one-time first-run setup.
    Private isFirstRun As Boolean = False

    ' --- Theme state ---
    Private isDarkMode As Boolean = False

    ' --- Screenshot monitor: saves Prt Sc captures while the app runs ---
    ' Captures land in Pictures\Screens by default, or in a folder the user chooses
    ' via the drop-box on the All Links tab (persisted in the layout XML).
    Private ReadOnly screenshotMonitor As New ScreenshotMonitor()

    ' The user's chosen screenshot folder, or Nothing to use the default.
    Private screenshotFolderOverride As String = Nothing

    ' Timer that restores the title bar color after the green "saved" flash.
    Private titleFlashTimer As Timer

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

    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
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

        ' Wire up Tab 5 buttons.
        AddHandler btnRestoreBackup.Click, AddressOf RestoreBackup_Click
        AddHandler btnDarkModeToggle.Click, AddressOf DarkModeToggle_Click
        AddHandler btnCreateAppShortcuts.Click, AddressOf CreateAppShortcuts_Click
        AddHandler lvwAllLinks.DoubleClick, AddressOf AllLinksListView_DoubleClick

        ' Wire up the link editor under the All Links list.
        AddHandler lvwAllLinks.SelectedIndexChanged, AddressOf AllLinksList_SelectedIndexChanged
        AddHandler cboLinkTab.SelectedIndexChanged, AddressOf LinkEditorField_Changed
        AddHandler txtLinkName.TextChanged, AddressOf LinkEditorField_Changed
        AddHandler txtLinkPath.TextChanged, AddressOf LinkEditorField_Changed
        AddHandler txtLinkNote.TextChanged, AddressOf LinkEditorField_Changed
        AddHandler txtLinkPath.DragEnter, AddressOf LinkPath_DragEnter
        AddHandler txtLinkPath.DragDrop, AddressOf LinkPath_DragDrop
        AddHandler btnLinkBrowseFile.Click, AddressOf LinkBrowseFile_Click
        AddHandler btnLinkBrowseFolder.Click, AddressOf LinkBrowseFolder_Click
        AddHandler btnLinkSave.Click, Sub() SaveEditor()
        AddHandler btnLinkRevert.Click, Sub() LoadEditor(editingIcon)
        AddHandler btnLinkRemove.Click, AddressOf LinkRemove_Click
        AddHandler btnLinkLaunch.Click, Sub() LaunchIcon(editingIcon)
        AddHandler btnLinkOpenFolder.Click, Sub() If editingIcon IsNot Nothing Then OpenContainingFolder(editingIcon.Tag.ToString())

        ' Wire up the screenshot-folder drop-box and its reset button.
        AddHandler txtScreenshotFolder.DragEnter, AddressOf ScreenshotFolder_DragEnter
        AddHandler txtScreenshotFolder.DragDrop, AddressOf ScreenshotFolder_DragDrop
        AddHandler btnResetScreenshotFolder.Click, AddressOf ResetScreenshotFolder_Click
        RefreshScreenshotFolderDisplay()

        ' Wire up tab drawing and tab change.
        AddHandler TabControl1.DrawItem, AddressOf TabControl1_DrawItem
        AddHandler TabControl1.SelectedIndexChanged, AddressOf TabControl1_SelectedIndexChanged
        AddHandler TabControl1.Deselecting, AddressOf TabControl1_Deselecting

        ' Build the tab-header right-click menu.
        AddHandler mnuTabAdd.Click, Sub() AddTabAfter(tabMenuTarget)
        AddHandler mnuTabRename.Click, Sub() PromptRenameTab(tabMenuTarget)
        AddHandler mnuTabMoveLeft.Click, Sub() MoveTab(tabMenuTarget, -1)
        AddHandler mnuTabMoveRight.Click, Sub() MoveTab(tabMenuTarget, 1)
        AddHandler mnuTabDelete.Click, Sub() DeleteTab(tabMenuTarget)
        TabHeaderMenu.Items.AddRange({mnuTabAdd, mnuTabRename, New ToolStripSeparator(),
                                      mnuTabMoveLeft, mnuTabMoveRight, New ToolStripSeparator(), mnuTabDelete})
        AddHandler TabControl1.MouseUp, AddressOf TabControl1_MouseUp

        ' Wire up the tab list on All Links (rename in place by clicking a selected name, or F2/Rename).
        AddHandler btnTabAdd.Click, Sub() AddTabAfter(SelectedListTab())
        AddHandler btnTabRename.Click, Sub() lvwTabs.SelectedItems.Cast(Of ListViewItem)().FirstOrDefault()?.BeginEdit()
        AddHandler btnTabMoveUp.Click, Sub() MoveTab(SelectedListTab(), -1)
        AddHandler btnTabMoveDown.Click, Sub() MoveTab(SelectedListTab(), 1)
        AddHandler btnTabDelete.Click, Sub() DeleteTab(SelectedListTab())
        AddHandler lvwTabs.SelectedIndexChanged, Sub() UpdateTabButtons()
        AddHandler lvwTabs.AfterLabelEdit, AddressOf TabList_AfterLabelEdit
        AddHandler lvwTabs.KeyDown, AddressOf TabList_KeyDown
        AddHandler lvwTabs.DoubleClick, Sub() If SelectedListTab() IsNot Nothing Then TabControl1.SelectedTab = SelectedListTab()

        isFirstRun = Not IO.File.Exists(LayoutFilePath)
        BackupXmlFiles()
        LoadLayout()
        TabControl1.SelectedIndex = 0
        LoadEditor(Nothing)

        ' Start watching for the Prt Sc key for the lifetime of the app.
        AddHandler screenshotMonitor.ScreenshotSaved, AddressOf OnScreenshotSaved
        screenshotMonitor.StartMonitoring()
    End Sub

    ''' <summary>
    ''' Applies the given folder as the screenshot save location (Nothing/blank or a
    ''' non-existent path falls back to the default Pictures\Screens) and refreshes
    ''' the drop-box display on the All Links tab.
    ''' </summary>
    Private Sub SetScreenshotFolder(folder As String)
        If String.IsNullOrWhiteSpace(folder) OrElse Not IO.Directory.Exists(folder) Then
            screenshotFolderOverride = Nothing
        Else
            screenshotFolderOverride = folder
        End If

        screenshotMonitor.SaveFolder = screenshotFolderOverride
        RefreshScreenshotFolderDisplay()
    End Sub

    ''' <summary>Shows the active screenshot folder, flagging the default case.</summary>
    Private Sub RefreshScreenshotFolderDisplay()
        If screenshotFolderOverride Is Nothing Then
            txtScreenshotFolder.Text = ScreenshotMonitor.DefaultSaveFolder & "  (default)"
        Else
            txtScreenshotFolder.Text = screenshotFolderOverride
        End If
    End Sub

    ' --- Screenshot folder drop-box (All Links tab) ---

    Private Sub ScreenshotFolder_DragEnter(sender As Object, e As DragEventArgs)
        ' Accept a single folder drop only.
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            Dim paths As String() = CType(e.Data.GetData(DataFormats.FileDrop), String())
            If paths IsNot Nothing AndAlso paths.Length > 0 AndAlso IO.Directory.Exists(paths(0)) Then
                e.Effect = DragDropEffects.Copy
                Return
            End If
        End If
        e.Effect = DragDropEffects.None
    End Sub

    Private Sub ScreenshotFolder_DragDrop(sender As Object, e As DragEventArgs)
        If Not e.Data.GetDataPresent(DataFormats.FileDrop) Then Return
        Dim paths As String() = CType(e.Data.GetData(DataFormats.FileDrop), String())
        If paths Is Nothing OrElse paths.Length = 0 Then Return

        Dim folder As String = paths(0)
        If Not IO.Directory.Exists(folder) Then
            MessageBox.Show("Please drop a folder (not a file) to set the screenshot save location.",
                            "Not a Folder", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        SetScreenshotFolder(folder)
        SaveLayout()
    End Sub

    Private Sub ResetScreenshotFolder_Click(sender As Object, e As EventArgs)
        SetScreenshotFolder(Nothing)
        SaveLayout()
    End Sub

    ' Flash the title bar green when a screenshot lands in the Screens folder.
    ' The event fires on a background thread, so marshal back to the UI thread.
    Private Sub OnScreenshotSaved(sender As Object, e As EventArgs)
        If IsHandleCreated Then
            BeginInvoke(New Action(AddressOf FlashTitleBarSaved))
        End If
    End Sub

    Private Sub FlashTitleBarSaved()
        ' Set the caption to green now; a one-shot timer restores the default.
        Dim green As Integer = ColorTranslator.ToWin32(Color.FromArgb(46, 204, 113))
        DwmSetWindowAttribute(Me.Handle, DWMWA_CAPTION_COLOR, green, 4)

        If titleFlashTimer Is Nothing Then
            titleFlashTimer = New Timer() With {.Interval = 600}
            AddHandler titleFlashTimer.Tick,
                Sub()
                    titleFlashTimer.Stop()
                    Dim defaultColor As Integer = DWMWA_COLOR_DEFAULT
                    DwmSetWindowAttribute(Me.Handle, DWMWA_CAPTION_COLOR, defaultColor, 4)
                End Sub
        End If

        ' Restart the timer so rapid captures keep the flash visible, then settle.
        titleFlashTimer.Stop()
        titleFlashTimer.Start()
    End Sub

    Private Sub TabControl1_DrawItem(sender As Object, e As DrawItemEventArgs)
        Dim tc As TabControl = CType(sender, TabControl)

        ' While a tab is being inserted on a visible form, Windows can ask to paint the new
        ' header before WinForms has added it to TabPages; skip that paint (another follows).
        If e.Index < 0 OrElse e.Index >= tc.TabPages.Count Then Return

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

    Private Sub frmMain_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Unsaved link edits: ask (unless Windows itself is shutting down); Cancel keeps the app open.
        If e.CloseReason = CloseReason.UserClosing AndAlso Not ConfirmPendingEdit() Then
            e.Cancel = True
            Return
        End If

        SaveLayout()
        SaveFormSettings()
        screenshotMonitor.Dispose()
        titleFlashTimer?.Dispose()
    End Sub

    ' --- Grid tabs ---

    ''' <summary>
    ''' Builds a new icon-grid tab (a 4x4 grid of empty drop cells) and inserts it
    ''' just before the All Links tab. Returns the new page; its grid is in page.Tag.
    ''' </summary>
    Private Function AddGridTab(tabName As String, Optional index As Integer = -1) As TabPage
        Dim grid As New TableLayoutPanel() With {
            .ColumnCount = GridColumns,
            .RowCount = GridRows,
            .Dock = DockStyle.Fill
        }
        For i As Integer = 1 To GridColumns
            grid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F / GridColumns))
        Next
        For i As Integer = 1 To GridRows
            grid.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F / GridRows))
        Next
        SetupTableCells(grid)

        Dim page As New TabPage(tabName) With {
            .Padding = New Padding(3),
            .UseVisualStyleBackColor = True,
            .Tag = grid
        }
        page.Controls.Add(grid)

        ' Grid tabs always sit before All Links; -1 (or anything past it) means "last".
        Dim lastSlot As Integer = TabControl1.TabPages.IndexOf(tabAllLinks)
        If index < 0 OrElse index > lastSlot Then index = lastSlot
        tabChangeDepth += 1
        Try
            TabControl1.TabPages.Insert(index, page)
        Finally
            tabChangeDepth -= 1
        End Try
        Return page
    End Function

    Private Sub CreateDefaultTabs()
        For Each tabName As String In DefaultTabNames
            AddGridTab(tabName)
        Next
    End Sub

    ''' <summary>All icon-grid tabs in display order (every tab except All Links).</summary>
    Private Function GridTabPages() As List(Of TabPage)
        Dim pages As New List(Of TabPage)()
        For Each page As TabPage In TabControl1.TabPages
            If TypeOf page.Tag Is TableLayoutPanel Then pages.Add(page)
        Next
        Return pages
    End Function

    ''' <summary>The icon grid on a tab, or Nothing for All Links.</summary>
    Private Shared Function GridOf(page As TabPage) As TableLayoutPanel
        Return TryCast(page?.Tag, TableLayoutPanel)
    End Function

    ' --- Tab management: tab-header right-click menu and the tab list on All Links ---

    Private Sub TabControl1_MouseUp(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Right Then Return

        For i As Integer = 0 To TabControl1.TabCount - 1
            If Not TabControl1.GetTabRect(i).Contains(e.Location) Then Continue For

            Dim page As TabPage = TabControl1.TabPages(i)
            Dim pages As List(Of TabPage) = GridTabPages()
            Dim gridIndex As Integer = pages.IndexOf(page)   ' -1 for All Links

            ' On All Links only "Add Tab" applies (it adds a tab at the end).
            tabMenuTarget = If(gridIndex >= 0, page, Nothing)
            mnuTabRename.Enabled = gridIndex >= 0
            mnuTabMoveLeft.Enabled = gridIndex > 0
            mnuTabMoveRight.Enabled = gridIndex >= 0 AndAlso gridIndex < pages.Count - 1
            mnuTabDelete.Enabled = gridIndex >= 0 AndAlso pages.Count > 1
            TabHeaderMenu.Show(TabControl1, e.Location)
            Return
        Next
    End Sub

    ''' <summary>The grid tab selected in the All Links tab list, or Nothing.</summary>
    Private Function SelectedListTab() As TabPage
        If lvwTabs.SelectedItems.Count = 0 Then Return Nothing
        Return TryCast(lvwTabs.SelectedItems(0).Tag, TabPage)
    End Function

    ''' <summary>
    ''' Rebuilds the All Links tab list (name and link count per tab), keeping the given
    ''' tab (or the current one) selected.
    ''' </summary>
    Private Sub RefreshTabList(Optional selectPage As TabPage = Nothing)
        Dim keep As TabPage = If(selectPage, SelectedListTab())
        Dim keepItem As ListViewItem = Nothing

        lvwTabs.BeginUpdate()
        lvwTabs.Items.Clear()
        For Each page As TabPage In GridTabPages()
            Dim item As New ListViewItem(page.Text) With {.Tag = page}
            item.SubItems.Add(OccupiedCells(GridOf(page)).Count.ToString())
            lvwTabs.Items.Add(item)
            If page Is keep Then keepItem = item
        Next
        lvwTabs.EndUpdate()

        If keepItem IsNot Nothing Then
            keepItem.Selected = True
            keepItem.Focused = True
            keepItem.EnsureVisible()
        End If
        UpdateTabButtons()
    End Sub

    Private Sub UpdateTabButtons()
        Dim pages As List(Of TabPage) = GridTabPages()
        Dim index As Integer = pages.IndexOf(SelectedListTab())   ' -1 when nothing is selected
        btnTabRename.Enabled = index >= 0
        btnTabMoveUp.Enabled = index > 0
        btnTabMoveDown.Enabled = index >= 0 AndAlso index < pages.Count - 1
        btnTabDelete.Enabled = index >= 0 AndAlso pages.Count > 1
    End Sub

    ''' <summary>Saves, then refreshes both lists on All Links so they show the new tab set.</summary>
    Private Sub AfterTabsChanged(selectPage As TabPage)
        SaveLayout()
        RefreshTabList(selectPage)
        If TabControl1.SelectedTab Is tabAllLinks Then RefreshAllLinksList()
    End Sub

    Private Function NextDefaultTabName() As String
        Dim names As New HashSet(Of String)(GridTabPages().Select(Function(p) p.Text), StringComparer.OrdinalIgnoreCase)
        Dim n As Integer = names.Count + 1
        While names.Contains("Tab " & n)
            n += 1
        End While
        Return "Tab " & n
    End Function

    ''' <summary>
    ''' Asks for a name and adds an empty grid tab just after the given tab
    ''' (or at the end when Nothing). A tab added from a grid tab's header is opened.
    ''' </summary>
    Private Sub AddTabAfter(page As TabPage)
        Dim tabName As String = ShowTextPrompt("Add Tab", "Name for the new tab:", NextDefaultTabName())
        If tabName Is Nothing Then Return

        Dim index As Integer = If(GridOf(page) IsNot Nothing, TabControl1.TabPages.IndexOf(page) + 1, -1)
        Dim newPage As TabPage = AddGridTab(tabName, index)
        ApplyTheme()
        If TabControl1.SelectedTab IsNot tabAllLinks Then TabControl1.SelectedTab = newPage
        AfterTabsChanged(newPage)
    End Sub

    Private Sub PromptRenameTab(page As TabPage)
        If GridOf(page) Is Nothing Then Return
        Dim newName As String = ShowTextPrompt("Rename Tab", "New name for this tab:", page.Text)
        If newName Is Nothing Then Return
        page.Text = newName
        AfterTabsChanged(page)
    End Sub

    Private Sub TabList_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.F2 AndAlso lvwTabs.SelectedItems.Count > 0 Then
            lvwTabs.SelectedItems(0).BeginEdit()
        End If
    End Sub

    Private Sub TabList_AfterLabelEdit(sender As Object, e As LabelEditEventArgs)
        ' The list is rebuilt from the tabs below, so never let the ListView keep its own edit.
        e.CancelEdit = True
        If e.Label Is Nothing Then Return   ' edit cancelled (Esc, or no change)

        Dim newName As String = e.Label.Trim()
        Dim page As TabPage = TryCast(lvwTabs.Items(e.Item).Tag, TabPage)
        If page Is Nothing OrElse newName.Length = 0 Then Return

        ' Rebuilding the list from inside its own edit event is unsafe; do it right after.
        BeginInvoke(Sub()
                        page.Text = newName
                        AfterTabsChanged(page)
                    End Sub)
    End Sub

    ''' <summary>Moves a grid tab one place left (-1) or right (+1); All Links always stays last.</summary>
    Private Sub MoveTab(page As TabPage, delta As Integer)
        If GridOf(page) Is Nothing Then Return
        Dim pages As List(Of TabPage) = GridTabPages()
        Dim newIndex As Integer = pages.IndexOf(page) + delta
        If newIndex < 0 OrElse newIndex >= pages.Count Then Return

        ' Grid tabs occupy the first TabPages slots, so a grid index is also a TabPages index.
        Dim selected As TabPage = TabControl1.SelectedTab
        tabChangeDepth += 1
        Try
            TabControl1.TabPages.Remove(page)
            TabControl1.TabPages.Insert(newIndex, page)
            TabControl1.SelectedTab = selected
        Finally
            tabChangeDepth -= 1
        End Try
        AfterTabsChanged(page)
    End Sub

    ''' <summary>
    ''' Deletes a grid tab. If it holds links, the user chooses to move them to another tab
    ''' (the default) or delete them too. A copy of the layout is saved to BACKUP-XML first.
    ''' </summary>
    Private Sub DeleteTab(page As TabPage)
        Dim grid As TableLayoutPanel = GridOf(page)
        If grid Is Nothing Then Return

        Dim others As List(Of TabPage) = GridTabPages().Where(Function(p) p IsNot page).ToList()
        If others.Count = 0 Then
            MessageBox.Show("This is the only tab, so it can't be deleted. Add another tab first.",
                            "Delete Tab", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim linkCells As List(Of Panel) = OccupiedCells(grid)
        Dim moveTarget As TabPage = Nothing

        If linkCells.Count = 0 Then
            If MessageBox.Show($"Delete the empty tab ""{page.Text}""?", "Delete Tab", MessageBoxButtons.YesNo,
                               MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
        Else
            If Not ShowDeleteTabDialog(page, linkCells.Count, others, moveTarget) Then Return

            If moveTarget IsNot Nothing Then
                Dim freeCells As Integer = EmptyCellCount(GridOf(moveTarget))
                If freeCells < linkCells.Count Then
                    MessageBox.Show($"""{moveTarget.Text}"" has {freeCells} free cells, not enough for {linkCells.Count} links." & vbCrLf & vbCrLf &
                                    "Pick another tab, or add a new tab and delete this one again.",
                                    "Delete Tab", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
            End If
        End If

        Try
            SnapshotLayout("BeforeDelete")
        Catch ex As Exception
            MessageBox.Show("The tab was not deleted because a backup copy of the layout couldn't be saved first." &
                            vbCrLf & vbCrLf & ex.Message, "Delete Tab", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End Try

        For Each sourceCell As Panel In linkCells
            If moveTarget IsNot Nothing Then
                ' Move the icon and its label as-is, so notes and missing-link state come along.
                Dim targetCell As Panel = FindFirstEmptyCell(GridOf(moveTarget))
                Dim moving As New List(Of Control)(sourceCell.Controls.Cast(Of Control)())
                sourceCell.Controls.Clear()
                For Each c As Control In moving
                    targetCell.Controls.Add(c)
                Next
            Else
                For Each pb As PictureBox In sourceCell.Controls.OfType(Of PictureBox)()
                    iconNotes.Remove(pb)
                    iconNames.Remove(pb)
                    missingLinks.Remove(pb)
                Next
            End If
        Next

        Dim wasSelected As Boolean = TabControl1.SelectedTab Is page
        tabChangeDepth += 1
        Try
            TabControl1.TabPages.Remove(page)
            page.Dispose()
            If wasSelected Then TabControl1.SelectedTab = If(moveTarget, others(0))
        Finally
            tabChangeDepth -= 1
        End Try
        AfterTabsChanged(moveTarget)
    End Sub

    ''' <summary>Grid cells that hold a link, in row-major order.</summary>
    Private Function OccupiedCells(tlp As TableLayoutPanel) As List(Of Panel)
        Dim cells As New List(Of Panel)()
        For row As Integer = 0 To tlp.RowCount - 1
            For col As Integer = 0 To tlp.ColumnCount - 1
                Dim cell As Panel = TryCast(tlp.GetControlFromPosition(col, row), Panel)
                If cell IsNot Nothing AndAlso cell.Controls.OfType(Of PictureBox)().Any() Then cells.Add(cell)
            Next
        Next
        Return cells
    End Function

    Private Function EmptyCellCount(tlp As TableLayoutPanel) As Integer
        Return tlp.Controls.OfType(Of Panel)().Count(Function(cell) cell.Controls.Count = 0)
    End Function

    ''' <summary>
    ''' Saves what's on screen, then copies the layout file into BACKUP-XML as
    ''' "{prefix}-ProgramManagerLayout_{timestamp}.xml". Those names aren't matched by the
    ''' rolling-backup prune, so the copies are kept until the user deletes them.
    ''' </summary>
    Private Sub SnapshotLayout(prefix As String)
        SaveLayout()
        If Not IO.File.Exists(LayoutFilePath) Then Return

        Dim backupDir As String = IO.Path.Combine(Application.StartupPath, "BACKUP-XML")
        IO.Directory.CreateDirectory(backupDir)
        Dim stamp As String = DateTime.Now.ToString("yyyyMMdd_HHmmss")
        IO.File.Copy(LayoutFilePath, IO.Path.Combine(backupDir, $"{prefix}-ProgramManagerLayout_{stamp}.xml"), True)
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

    ' --- All Links tab event handlers ---

    ''' <summary>
    ''' Lets the user pick a layout backup (normally from BACKUP-XML) and reloads every grid
    ''' tab from it. The current layout is copied to a BeforeRestore- file first, so a restore
    ''' can itself be undone by restoring that file.
    ''' </summary>
    Private Sub RestoreBackup_Click(sender As Object, e As EventArgs)
        Dim backupDir As String = IO.Path.Combine(Application.StartupPath, "BACKUP-XML")

        Dim backupFile As String
        Using dlg As New OpenFileDialog() With {
            .Title = "Restore Layout from Backup",
            .InitialDirectory = If(IO.Directory.Exists(backupDir), backupDir, Application.StartupPath),
            .Filter = "Layout backups|*ProgramManagerLayout*.xml|All XML files|*.xml"
        }
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
            backupFile = dlg.FileName
        End Using

        ' Make sure the backup is readable before anything on screen or disk is touched.
        Dim tabCount As Integer
        Dim linkCount As Integer
        Try
            Dim doc As New XmlDocument()
            doc.Load(backupFile)
            tabCount = doc.SelectNodes("//Tabs/Tab").Count
            linkCount = doc.SelectNodes("//Icon").Count
            If tabCount = 0 AndAlso doc.SelectNodes("//Icons/Icon").Count = 0 Then
                Throw New IO.InvalidDataException("No tabs or links were found in this file.")
            End If
        Catch ex As Exception
            MessageBox.Show("This file can't be restored." & vbCrLf & vbCrLf & ex.Message,
                            "Restore from Backup", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End Try

        Dim confirm As String =
            "Replace the current layout with this backup?" & vbCrLf & vbCrLf &
            IO.Path.GetFileName(backupFile) & vbCrLf &
            $"Saved {IO.File.GetLastWriteTime(backupFile):g}  -  {Math.Max(tabCount, 1)} tabs, {linkCount} links" & vbCrLf & vbCrLf &
            "Your current layout will be backed up first, so you can switch back."
        If MessageBox.Show(confirm, "Restore from Backup", MessageBoxButtons.YesNo,
                           MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return

        Try
            ' Keep a copy of what's on screen so this restore can be undone.
            SnapshotLayout("BeforeRestore")

            ' Swap the backup in the same way SaveLayout does (temp file, then replace).
            Dim tempPath As String = LayoutFilePath & ".tmp"
            IO.File.Copy(backupFile, tempPath, True)
            IO.File.Move(tempPath, LayoutFilePath, True)
        Catch ex As Exception
            MessageBox.Show("Could not restore the backup: " & ex.Message,
                            "Restore from Backup", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        ReloadLayout()
    End Sub

    ''' <summary>Discards the grid tabs on screen and rebuilds them from the layout file.</summary>
    Private Sub ReloadLayout()
        isReloadingLayout = True
        Try
            ' Any edit in progress belongs to the layout being replaced.
            editingIcon = Nothing
            TabControl1.SelectedTab = tabAllLinks
            For Each page As TabPage In GridTabPages()
                TabControl1.TabPages.Remove(page)
                page.Dispose()
            Next
            iconNotes.Clear()
            iconNames.Clear()
            missingLinks.Clear()
            contextTarget = Nothing
            layoutLoadFailed = False

            ' The restored file's own screenshot folder (if it has one) is applied by LoadLayout.
            SetScreenshotFolder(Nothing)
            LoadLayout()
        Finally
            isReloadingLayout = False
        End Try

        TabControl1.SelectedTab = tabAllLinks
        RefreshAllLinksList()
        RefreshTabList()
    End Sub

    Private Sub DarkModeToggle_Click(sender As Object, e As EventArgs)
        isDarkMode = Not isDarkMode
        ApplyTheme()
        SaveLayout()
    End Sub

    ' --- All Apps shortcuts (Create-AppShortcuts.ps1, shipped next to the exe) ---

    ' Where the script puts the shortcuts; the app passes it in so both always agree.
    Private ReadOnly AppShortcutsFolder As String = IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "All Apps Shortcuts")

    Private Async Sub CreateAppShortcuts_Click(sender As Object, e As EventArgs)
        If MessageBox.Show("Create a shortcut to every app in the Windows All Apps list in:" & vbCrLf & vbCrLf &
                           AppShortcutsFolder & vbCrLf & vbCrLf &
                           "Shortcuts from a previous run in that folder are replaced. This can take a minute.",
                           "Create App Shortcuts", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) <> DialogResult.OK Then
            Return
        End If

        Dim result = Await RunAppShortcutsScriptAsync()
        If IsDisposed Then Return
        If Not result.Ok Then
            MessageBox.Show(result.ErrorText, "Create App Shortcuts", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        Try
            Process.Start("explorer.exe", """" & AppShortcutsFolder & """")
        Catch ex As Exception
            MessageBox.Show("The shortcuts were created in:" & vbCrLf & AppShortcutsFolder & vbCrLf & vbCrLf &
                            "but File Explorer could not be opened: " & ex.Message,
                            "Create App Shortcuts", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End Try

        If result.Failed > 0 Then
            MessageBox.Show($"{result.Failed} app(s) could not get a shortcut. The rest were created in:" & vbCrLf & AppShortcutsFolder,
                            "Create App Shortcuts", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    ''' <summary>
    ''' Runs Create-AppShortcuts.ps1 hidden, building <see cref="AppShortcutsFolder"/> with a shortcut
    ''' to every app in Windows' All Apps list. The Create App Shortcuts button is disabled while it
    ''' runs. Ok is False (with ErrorText set) when the script is missing or fails; Failed is the
    ''' number of apps that couldn't get a shortcut.
    ''' </summary>
    Private Async Function RunAppShortcutsScriptAsync() As Task(Of (Ok As Boolean, ErrorText As String, Failed As Integer))
        Dim scriptPath As String = IO.Path.Combine(Application.StartupPath, "Create-AppShortcuts.ps1")
        If Not IO.File.Exists(scriptPath) Then
            Return (False, "Create-AppShortcuts.ps1 was not found next to ProgramManager.exe:" & vbCrLf & vbCrLf & scriptPath, 0)
        End If

        Dim startInfo As New ProcessStartInfo(
            IO.Path.Combine(Environment.SystemDirectory, "WindowsPowerShell\v1.0\powershell.exe")) With {
            .UseShellExecute = False,
            .CreateNoWindow = True,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True
        }
        For Each arg As String In {"-NoProfile", "-ExecutionPolicy", "Bypass", "-File", scriptPath, "-Destination", AppShortcutsFolder}
            startInfo.ArgumentList.Add(arg)
        Next

        Dim buttonText As String = btnCreateAppShortcuts.Text
        btnCreateAppShortcuts.Enabled = False
        btnCreateAppShortcuts.Text = "Creating Shortcuts..."
        UseWaitCursor = True
        Dim output As String = ""
        Dim errors As String = ""
        Dim exitCode As Integer
        Try
            Using proc As Process = Process.Start(startInfo)
                ' Read both streams while waiting so a full pipe can't stall the script.
                Dim outputTask = proc.StandardOutput.ReadToEndAsync()
                Dim errorTask = proc.StandardError.ReadToEndAsync()
                Await proc.WaitForExitAsync()
                output = Await outputTask
                errors = Await errorTask
                exitCode = proc.ExitCode
            End Using
        Catch ex As Exception
            Return (False, "Could not run PowerShell." & vbCrLf & vbCrLf & ex.Message, 0)
        Finally
            If Not IsDisposed Then
                UseWaitCursor = False
                btnCreateAppShortcuts.Text = buttonText
                btnCreateAppShortcuts.Enabled = True
            End If
        End Try

        If exitCode <> 0 OrElse Not IO.Directory.Exists(AppShortcutsFolder) Then
            Dim detail As String = If(errors.Trim() <> "", errors.Trim(), output.Trim())
            If detail.Length > 1500 Then detail = detail.Substring(0, 1500) & "..."
            Return (False, "The shortcut script failed." & vbCrLf & vbCrLf & detail, 0)
        End If

        ' The script ends with "N failed" when some apps couldn't get a shortcut.
        Dim failed As Match = Regex.Match(output, "^(\d+) failed", RegexOptions.Multiline)
        Return (True, "", If(failed.Success, Integer.Parse(failed.Groups(1).Value), 0))
    End Function

    ' --- First run ---

    ''' <summary>
    ''' On the very first run (no layout file at startup): builds the All Apps shortcuts folder
    ''' and puts a link to it on the first tab, and offers to start Program Manager with Windows.
    ''' The script runs in the background while the Startup question is on screen.
    ''' </summary>
    Private Async Sub frmMain_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        If Not isFirstRun Then Return
        isFirstRun = False

        Dim scriptTask = RunAppShortcutsScriptAsync()
        OfferStartupShortcut()

        Dim result = Await scriptTask
        If IsDisposed Then Return
        If Not result.Ok Then
            MessageBox.Show("Program Manager couldn't create the All Apps shortcuts folder." & vbCrLf & vbCrLf &
                            result.ErrorText & vbCrLf & vbCrLf &
                            "You can try again later with Create App Shortcuts... on the All Links tab.",
                            "Program Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim pages As List(Of TabPage) = GridTabPages()
        If pages.Count = 0 Then Return
        Dim cell As Panel = FindFirstEmptyCell(GridOf(pages(0)))
        If cell Is Nothing Then Return
        PlaceIconInCell(cell, AppShortcutsFolder)
        ApplyTheme()   ' the new icon's label starts with light-theme colors
        SaveLayout()
        RefreshAllLinksList()
        RefreshTabList()
    End Sub

    ''' <summary>
    ''' Asks whether to add a Program Manager shortcut to the user's Startup folder (shell:startup)
    ''' so it starts at sign-in. Skipped when that shortcut already exists.
    ''' </summary>
    Private Sub OfferStartupShortcut()
        Dim lnkPath As String = IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Program Manager.lnk")
        If IO.File.Exists(lnkPath) Then Return

        If MessageBox.Show("Start Program Manager automatically when you sign in to Windows?" & vbCrLf & vbCrLf &
                           "This adds a shortcut to your Startup folder (shell:startup). " &
                           "You can delete it from there at any time.",
                           "Program Manager", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
            Return
        End If

        Try
            ShellShortcut.Create(lnkPath, Environment.ProcessPath, Application.StartupPath, "Program Manager")
        Catch ex As Exception
            MessageBox.Show("Could not add Program Manager to the Startup folder." & vbCrLf & vbCrLf & ex.Message,
                            "Program Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub ApplyTheme()
        If isDarkMode Then
            btnDarkModeToggle.Text = "Switch to Light Mode"
            Me.BackColor = DarkFormBack
            TabControl1.BackColor = DarkFormBack

            ' Style the icon grid tabs
            For Each tabPage As TabPage In GridTabPages()
                tabPage.BackColor = DarkTabBack
                tabPage.ForeColor = DarkForeColor

                Dim tlp As TableLayoutPanel = GridOf(tabPage)
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
            tabAllLinks.BackColor = DarkTabBack
            tabAllLinks.ForeColor = DarkForeColor
            StyleTab5Controls(DarkTabBack, DarkControlBack, DarkForeColor)
        Else
            btnDarkModeToggle.Text = "Switch to Dark Mode"
            Me.BackColor = LightFormBack
            TabControl1.BackColor = LightFormBack

            For Each tabPage As TabPage In GridTabPages()
                tabPage.BackColor = SystemColors.Window
                tabPage.ForeColor = LightForeColor
                tabPage.UseVisualStyleBackColor = True

                Dim tlp As TableLayoutPanel = GridOf(tabPage)
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

            tabAllLinks.BackColor = SystemColors.Window
            tabAllLinks.ForeColor = LightForeColor
            tabAllLinks.UseVisualStyleBackColor = True
            StyleTab5Controls(SystemColors.Window, LightControlBack, LightForeColor)
        End If
    End Sub

    Private Sub StyleTab5Controls(backColor As Color, controlBack As Color, foreColor As Color)
        For Each ctrl As Control In tabAllLinks.Controls
            StyleControlRecursive(ctrl, backColor, controlBack, foreColor)
        Next
    End Sub

    Private Sub StyleControlRecursive(ctrl As Control, backColor As Color, controlBack As Color, foreColor As Color)
        ctrl.ForeColor = foreColor

        If TypeOf ctrl Is TextBox OrElse TypeOf ctrl Is ComboBox Then
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
        If TabControl1.SelectedTab Is tabAllLinks Then
            RefreshAllLinksList()
            ' Also refresh the tab list to reflect current names and link counts.
            RefreshTabList()
        End If
    End Sub

    Private Sub RefreshAllLinksList()
        Dim keepItem As ListViewItem = Nothing

        lvwAllLinks.BeginUpdate()
        lvwAllLinks.Items.Clear()
        For Each page As TabPage In GridTabPages()
            For Each cell As Panel In OccupiedCells(GridOf(page))
                For Each pb As PictureBox In cell.Controls.OfType(Of PictureBox)()
                    If pb.Tag Is Nothing Then Continue For
                    Dim noteText As String = ""
                    iconNotes.TryGetValue(pb, noteText)
                    Dim item As New ListViewItem(page.Text)
                    item.SubItems.Add(DisplayNameOf(pb))
                    item.SubItems.Add(pb.Tag.ToString())
                    ' One-line list cell: show a multi-line note's line breaks as " / ".
                    item.SubItems.Add(If(noteText, "").Replace(vbCrLf, vbLf).Replace(vbLf, " / "))
                    item.Tag = pb   ' the grid icon itself, so the editor can change it
                    lvwAllLinks.Items.Add(item)
                    If pb Is editingIcon Then keepItem = item
                Next
            Next
        Next
        lvwAllLinks.EndUpdate()

        ' Keep the row being edited selected; if its link is gone, clear the editor.
        If keepItem IsNot Nothing Then
            keepItem.Selected = True
            keepItem.Focused = True
            keepItem.EnsureVisible()
        End If
        If keepItem Is Nothing Then
            LoadEditor(Nothing)
        ElseIf IsEditorDirty() Then
            RefreshEditorTabChoices()   ' tab names may have changed; keep the user's typing
        Else
            LoadEditor(editingIcon)
        End If
    End Sub

    Private Sub AllLinksListView_DoubleClick(sender As Object, e As EventArgs)
        LaunchIcon(SelectedListIcon())
    End Sub

    ' --- Link editor (under the All Links list): edits the link on the selected row ---

    ''' <summary>The link (grid icon) behind the selected All Links row, or Nothing.</summary>
    Private Function SelectedListIcon() As PictureBox
        If lvwAllLinks.SelectedItems.Count = 0 Then Return Nothing
        Return TryCast(lvwAllLinks.SelectedItems(0).Tag, PictureBox)
    End Function

    ''' <summary>The grid tab a link sits on (icon -> cell -> grid -> tab page).</summary>
    Private Shared Function TabOfIcon(pb As PictureBox) As TabPage
        Return TryCast(pb?.Parent?.Parent?.Parent, TabPage)
    End Function

    ''' <summary>Name shown for a link: its custom name if it has one, else the file or folder name.</summary>
    Private Function DisplayNameOf(pb As PictureBox) As String
        Dim customName As String = Nothing
        If iconNames.TryGetValue(pb, customName) AndAlso Not String.IsNullOrEmpty(customName) Then Return customName
        Return DefaultDisplayName(pb.Tag.ToString())
    End Function

    Private Shared Function DefaultDisplayName(filePath As String) As String
        ' GetFileNameWithoutExtension works correctly even for missing paths and folders.
        If IO.Directory.Exists(filePath) Then Return IO.Path.GetFileName(filePath)
        Dim name As String = IO.Path.GetFileNameWithoutExtension(filePath)
        Return If(String.IsNullOrEmpty(name), IO.Path.GetFileName(filePath), name)
    End Function

    ''' <summary>A link's note with Windows line breaks, as the multi-line note box holds it.</summary>
    Private Function NoteForEditor(pb As PictureBox) As String
        Dim note As String = ""
        iconNotes.TryGetValue(pb, note)
        Return If(note, "").Replace(vbCrLf, vbLf).Replace(vbLf, vbCrLf)
    End Function

    Private Function SelectedEditorTab() As TabPage
        Dim index As Integer = cboLinkTab.SelectedIndex
        If index < 0 OrElse index >= editorTabPages.Count Then Return Nothing
        Return editorTabPages(index)
    End Function

    ''' <summary>Refills the Tab drop-down from the current tabs, keeping the chosen tab.</summary>
    Private Sub RefreshEditorTabChoices()
        Dim keep As TabPage = SelectedEditorTab()
        loadingEditor = True
        editorTabPages = GridTabPages()
        cboLinkTab.Items.Clear()
        For Each page As TabPage In editorTabPages
            cboLinkTab.Items.Add(page.Text)
        Next
        cboLinkTab.SelectedIndex = editorTabPages.IndexOf(keep)
        loadingEditor = False
        UpdateEditorState()
    End Sub

    ''' <summary>Fills the editor from a link (Nothing clears and disables it).</summary>
    Private Sub LoadEditor(pb As PictureBox)
        editingIcon = pb
        loadingEditor = True
        editorTabPages = GridTabPages()
        cboLinkTab.Items.Clear()
        For Each page As TabPage In editorTabPages
            cboLinkTab.Items.Add(page.Text)
        Next

        If pb Is Nothing Then
            cboLinkTab.SelectedIndex = -1
            txtLinkName.Text = ""
            txtLinkPath.Text = ""
            txtLinkNote.Text = ""
        Else
            Dim customName As String = ""
            iconNames.TryGetValue(pb, customName)
            cboLinkTab.SelectedIndex = editorTabPages.IndexOf(TabOfIcon(pb))
            txtLinkName.Text = If(customName, "")
            txtLinkPath.Text = pb.Tag.ToString()
            txtLinkNote.Text = NoteForEditor(pb)
        End If
        loadingEditor = False
        UpdateEditorState()
    End Sub

    Private Function IsEditorDirty() As Boolean
        If editingIcon Is Nothing Then Return False
        Dim customName As String = ""
        iconNames.TryGetValue(editingIcon, customName)
        Return SelectedEditorTab() IsNot TabOfIcon(editingIcon) OrElse
               txtLinkName.Text.Trim() <> If(customName, "") OrElse
               txtLinkPath.Text.Trim() <> editingIcon.Tag.ToString() OrElse
               txtLinkNote.Text <> NoteForEditor(editingIcon)
    End Function

    ''' <summary>Enables the editor for a loaded link, and Save/Revert only when something changed.</summary>
    Private Sub UpdateEditorState()
        Dim hasLink As Boolean = editingIcon IsNot Nothing
        For Each c As Control In New Control() {cboLinkTab, txtLinkName, txtLinkPath, txtLinkNote, btnLinkBrowseFile,
                                                btnLinkBrowseFolder, btnLinkRemove, btnLinkOpenFolder, btnLinkLaunch}
            c.Enabled = hasLink
        Next

        Dim dirty As Boolean = IsEditorDirty()
        btnLinkSave.Enabled = dirty
        btnLinkRevert.Enabled = dirty

        If Not hasLink Then
            lblLinkStatus.Text = "Select a link above to edit it."
        ElseIf dirty Then
            lblLinkStatus.Text = "Unsaved changes - Save or Revert."
        ElseIf missingLinks.Contains(editingIcon) Then
            lblLinkStatus.Text = "Missing: the file or folder can't be found right now. The link is kept."
        Else
            lblLinkStatus.Text = ""
        End If
    End Sub

    Private Sub LinkEditorField_Changed(sender As Object, e As EventArgs)
        If Not loadingEditor Then UpdateEditorState()
    End Sub

    Private Sub AllLinksList_SelectedIndexChanged(sender As Object, e As EventArgs)
        ' Moving to another row fires this twice (old row off, then new row on);
        ' handle the change once, after both have happened.
        If listSelectionPending Then Return
        listSelectionPending = True
        BeginInvoke(New Action(AddressOf ApplyListSelection))
    End Sub

    Private Sub ApplyListSelection()
        listSelectionPending = False
        Dim pb As PictureBox = SelectedListIcon()
        If pb Is editingIcon Then Return

        If Not ConfirmPendingEdit() Then
            SelectListRow(editingIcon)   ' stay on the link being edited
            Return
        End If
        LoadEditor(pb)
        SelectListRow(pb)
    End Sub

    Private Sub SelectListRow(pb As PictureBox)
        If pb Is Nothing Then Return
        For Each item As ListViewItem In lvwAllLinks.Items
            If item.Tag Is pb Then
                item.Selected = True
                item.Focused = True
                item.EnsureVisible()
                Return
            End If
        Next
    End Sub

    ''' <summary>
    ''' If the editor has unsaved changes, asks whether to save them first. Returns False when
    ''' the user cancels or the save can't be done, meaning "stay where you are".
    ''' </summary>
    Private Function ConfirmPendingEdit() As Boolean
        If Not IsEditorDirty() Then Return True

        Select Case MessageBox.Show($"Save your changes to ""{DisplayNameOf(editingIcon)}""?", "Unsaved Changes",
                                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)
            Case DialogResult.Yes
                Return SaveEditor()
            Case DialogResult.No
                LoadEditor(editingIcon)   ' throw the edits away
                Return True
            Case Else
                Return False
        End Select
    End Function

    Private Sub TabControl1_Deselecting(sender As Object, e As TabControlCancelEventArgs)
        ' Leaving All Links with unsaved link edits: ask first (not when code is rearranging tabs).
        If e.TabPage IsNot tabAllLinks OrElse isReloadingLayout OrElse tabChangeDepth > 0 Then Return
        If Not ConfirmPendingEdit() Then e.Cancel = True
    End Sub

    ''' <summary>
    ''' Applies the editor's values to its link: moves it to the chosen tab (first free cell),
    ''' sets the custom name and note, and rebuilds the icon if the path changed. Returns False
    ''' if nothing was saved.
    ''' </summary>
    Private Function SaveEditor() As Boolean
        Dim pb As PictureBox = editingIcon
        If pb Is Nothing OrElse Not IsEditorDirty() Then Return True

        Dim newPath As String = txtLinkPath.Text.Trim()
        Dim newName As String = txtLinkName.Text.Trim()
        Dim newNote As String = If(String.IsNullOrWhiteSpace(txtLinkNote.Text), "", txtLinkNote.Text)
        Dim targetPage As TabPage = SelectedEditorTab()

        If newPath.Length = 0 Then
            MessageBox.Show("Enter the path of the file or folder this link opens.", "Edit Link",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If

        Dim pathChanged As Boolean = newPath <> pb.Tag.ToString()
        If pathChanged AndAlso Not IO.File.Exists(newPath) AndAlso Not IO.Directory.Exists(newPath) Then
            If MessageBox.Show("Nothing exists at this path right now:" & vbCrLf & vbCrLf & newPath & vbCrLf & vbCrLf &
                               "If it's on a drive that's unplugged, you can save it anyway; the link shows the " &
                               "warning icon until the drive is back. Save anyway?",
                               "Edit Link", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                               MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return False
        End If

        ' Move to another tab first, so a full tab stops the save before anything changes.
        Dim cell As Panel = CType(pb.Parent, Panel)
        If targetPage IsNot Nothing AndAlso targetPage IsNot TabOfIcon(pb) Then
            Dim targetCell As Panel = FindFirstEmptyCell(GridOf(targetPage))
            If targetCell Is Nothing Then
                MessageBox.Show($"""{targetPage.Text}"" is full. Free a cell there or pick another tab.", "Edit Link",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return False
            End If
            Dim moving As New List(Of Control)(cell.Controls.Cast(Of Control)())
            cell.Controls.Clear()
            For Each c As Control In moving
                targetCell.Controls.Add(c)
            Next
            cell = targetCell
        End If

        ' A blank name, or one that matches the file name, just means "use the file name".
        If newName.Length = 0 OrElse newName = DefaultDisplayName(newPath) Then newName = ""

        If pathChanged Then
            ' Build the new icon off-grid first, so a failure leaves the old link in place.
            Using staging As New Panel()
                PlaceIconInCell(staging, newPath, newNote, newName)
                Dim newIcon As PictureBox = staging.Controls.OfType(Of PictureBox)().FirstOrDefault()
                If newIcon Is Nothing Then Return False

                RemoveIcon(pb)
                Dim built As New List(Of Control)(staging.Controls.Cast(Of Control)())
                staging.Controls.Clear()
                For Each c As Control In built
                    cell.Controls.Add(c)
                Next
                pb = newIcon
            End Using
            ApplyTheme()   ' the new icon's label starts with light-theme colors
        Else
            If newName.Length = 0 Then iconNames.Remove(pb) Else iconNames(pb) = newName
            If newNote.Length = 0 Then iconNotes.Remove(pb) Else iconNotes(pb) = newNote

            Dim nameLabel As Label = cell.Controls.OfType(Of Label)().FirstOrDefault()
            If nameLabel IsNot Nothing Then nameLabel.Text = DisplayNameOf(pb)
            If IO.Directory.Exists(newPath) Then UpdateHiddenIndicator(pb)   ' re-applies the (H) prefix
            pb.Invalidate()   ' note badge
        End If

        editingIcon = pb
        SaveLayout()
        RefreshAllLinksList()
        RefreshTabList()
        Return True
    End Function

    Private Sub LinkRemove_Click(sender As Object, e As EventArgs)
        Dim pb As PictureBox = editingIcon
        If pb Is Nothing Then Return
        If MessageBox.Show($"Remove the link ""{DisplayNameOf(pb)}"" from ""{TabOfIcon(pb)?.Text}""?" & vbCrLf & vbCrLf &
                           "The file or folder itself is not touched.", "Remove Link", MessageBoxButtons.YesNo,
                           MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return

        RemoveIcon(pb)
        SaveLayout()
        RefreshAllLinksList()
        RefreshTabList()
    End Sub

    Private Sub LinkBrowseFile_Click(sender As Object, e As EventArgs)
        Using dlg As New OpenFileDialog() With {
            .Title = "Choose the File for This Link",
            .Filter = "All files|*.*",
            .DereferenceLinks = False   ' keep a picked .lnk shortcut as the shortcut itself
        }
            Dim current As String = txtLinkPath.Text.Trim()
            Dim folder As String = If(IO.Directory.Exists(current), current, SafeDirectoryName(current))
            If folder IsNot Nothing AndAlso IO.Directory.Exists(folder) Then dlg.InitialDirectory = folder
            If dlg.ShowDialog(Me) = DialogResult.OK Then txtLinkPath.Text = dlg.FileName
        End Using
    End Sub

    Private Sub LinkBrowseFolder_Click(sender As Object, e As EventArgs)
        Using dlg As New FolderBrowserDialog() With {.Description = "Choose the folder for this link"}
            Dim current As String = txtLinkPath.Text.Trim()
            Dim folder As String = If(IO.Directory.Exists(current), current, SafeDirectoryName(current))
            If folder IsNot Nothing AndAlso IO.Directory.Exists(folder) Then dlg.InitialDirectory = folder
            If dlg.ShowDialog(Me) = DialogResult.OK Then txtLinkPath.Text = dlg.SelectedPath
        End Using
    End Sub

    Private Shared Function SafeDirectoryName(path As String) As String
        Try
            Return IO.Path.GetDirectoryName(path)
        Catch
            Return Nothing
        End Try
    End Function

    Private Sub LinkPath_DragEnter(sender As Object, e As DragEventArgs)
        e.Effect = If(e.Data.GetDataPresent(DataFormats.FileDrop), DragDropEffects.Copy, DragDropEffects.None)
    End Sub

    Private Sub LinkPath_DragDrop(sender As Object, e As DragEventArgs)
        Dim paths As String() = TryCast(e.Data.GetData(DataFormats.FileDrop), String())
        If paths IsNot Nothing AndAlso paths.Length > 0 Then txtLinkPath.Text = paths(0)
    End Sub

    ' --- Helper: get the active tab's TableLayoutPanel (Nothing on All Links) ---

    Private Function GetActiveTableLayoutPanel() As TableLayoutPanel
        Return GridOf(TabControl1.SelectedTab)
    End Function

    ' --- External File Drop (onto the form / table) ---

    Private Sub frmMain_DragEnter(sender As Object, e As DragEventArgs) Handles Me.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
        Else
            e.Effect = DragDropEffects.None
        End If
    End Sub

    Private Sub frmMain_DragDrop(sender As Object, e As DragEventArgs) Handles Me.DragDrop
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
    Private Sub PlaceIconInCell(cell As Panel, filePath As String, Optional note As String = "",
                                Optional customName As String = "")
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
                ' Extract the file's associated icon. If Windows can't supply one, use a
                ' generic icon rather than failing, so the link is never dropped from the grid.
                Dim fileIcon As Icon = Nothing
                Try
                    fileIcon = Icon.ExtractAssociatedIcon(filePath)
                Catch
                End Try
                iconBitmap = If(fileIcon IsNot Nothing, fileIcon.ToBitmap(), SystemIcons.Application.ToBitmap())
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

            ' Add a label below the icon showing the name (the custom name, if one was given).
            If Not String.IsNullOrEmpty(customName) Then
                iconNames(iconBox) = customName
            End If
            Dim displayName As String = DisplayNameOf(iconBox)

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
        LaunchIcon(CType(sender, PictureBox))
    End Sub

    ''' <summary>Launches a link, or explains why not when its file or folder is missing.</summary>
    Private Sub LaunchIcon(clickedIcon As PictureBox)
        If clickedIcon Is Nothing OrElse clickedIcon.Tag Is Nothing Then Return

        Dim filePath As String = clickedIcon.Tag.ToString()

        ' If the link is known-missing, inform the user rather than trying to launch.
        If missingLinks.Contains(clickedIcon) Then
            MessageBox.Show("This shortcut points to a file or folder that no longer exists." & vbCrLf & vbCrLf &
                            "Path: " & filePath & vbCrLf & vbCrLf &
                            "Remove it from the grid (right-click > Remove), or point it at the right path on the All Links tab.",
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

            ' One-time snapshot of the layout as it was before tabs became dynamic.
            ' Its name doesn't match the rolling-backup pattern, so it is never pruned.
            Dim layoutFile As String = IO.Path.Combine(Application.StartupPath, "ProgramManagerLayout.xml")
            Dim snapshot As String = IO.Path.Combine(backupDir, "PreDynamicTabs-ProgramManagerLayout.xml")
            If IO.File.Exists(layoutFile) AndAlso Not IO.File.Exists(snapshot) Then
                IO.File.Copy(layoutFile, snapshot)
            End If

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
        OpenContainingFolder(contextTarget.Tag.ToString())
    End Sub

    Private Sub OpenContainingFolder(filePath As String)
        Try
            Process.Start("explorer.exe", "/select, """ & filePath & """")
        Catch ex As Exception
            MessageBox.Show("Could not open folder." & vbCrLf & ex.Message,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub MnuRemove_Click(sender As Object, e As EventArgs)
        If contextTarget Is Nothing Then Return
        RemoveIcon(contextTarget)
        contextTarget = Nothing
        SaveLayout()
    End Sub

    ''' <summary>
    ''' Takes a link off its grid cell and forgets its note and custom name.
    ''' The file or folder it points to is not touched.
    ''' </summary>
    Private Sub RemoveIcon(pb As PictureBox)
        iconNotes.Remove(pb)
        iconNames.Remove(pb)
        missingLinks.Remove(pb)
        TryCast(pb.Parent, Panel)?.Controls.Clear()
        If pb Is editingIcon Then editingIcon = Nothing
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

    ''' <summary>
    ''' Asks for one line of text (a tab name). Returns the trimmed text, or Nothing if
    ''' cancelled; OK stays disabled while the box is blank.
    ''' </summary>
    Private Function ShowTextPrompt(title As String, prompt As String, initialText As String) As String
        Dim result As String = Nothing

        Using dlg As New Form()
            dlg.Text = title
            dlg.ClientSize = New Size(360, 112)
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog
            dlg.StartPosition = FormStartPosition.CenterParent
            dlg.MaximizeBox = False
            dlg.MinimizeBox = False
            dlg.ShowInTaskbar = False

            Dim lbl As New Label() With {.Text = prompt, .AutoSize = True, .Location = New Point(12, 12)}
            Dim txt As New TextBox() With {.Text = initialText, .Location = New Point(12, 36), .Width = 336}
            Dim btnOK As New Button() With {
                .Text = "OK", .DialogResult = DialogResult.OK,
                .Size = New Size(80, 30), .Location = New Point(182, 72)
            }
            Dim btnCancel As New Button() With {
                .Text = "Cancel", .DialogResult = DialogResult.Cancel,
                .Size = New Size(80, 30), .Location = New Point(268, 72)
            }
            btnOK.Enabled = txt.Text.Trim().Length > 0
            AddHandler txt.TextChanged, Sub() btnOK.Enabled = txt.Text.Trim().Length > 0

            dlg.Controls.AddRange({lbl, txt, btnOK, btnCancel})
            dlg.AcceptButton = btnOK
            dlg.CancelButton = btnCancel
            StyleDialog(dlg)

            txt.SelectAll()
            If dlg.ShowDialog(Me) = DialogResult.OK Then
                result = txt.Text.Trim()
            End If
        End Using

        Return result
    End Function

    ''' <summary>
    ''' Asks what to do with the links on a tab being deleted: move them to another tab
    ''' (the default) or delete them too. Returns False if cancelled; otherwise moveTarget is
    ''' the chosen tab, or Nothing when the user confirmed deleting the links.
    ''' </summary>
    Private Function ShowDeleteTabDialog(page As TabPage, linkCount As Integer, others As List(Of TabPage),
                                         ByRef moveTarget As TabPage) As Boolean
        Using dlg As New Form()
            dlg.Text = "Delete Tab"
            dlg.ClientSize = New Size(400, 176)
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog
            dlg.StartPosition = FormStartPosition.CenterParent
            dlg.MaximizeBox = False
            dlg.MinimizeBox = False
            dlg.ShowInTaskbar = False

            Dim lbl As New Label() With {
                .Text = $"""{page.Text}"" has {linkCount} link{If(linkCount = 1, "", "s")}. What should happen to them?",
                .AutoSize = True, .MaximumSize = New Size(376, 0), .Location = New Point(12, 12)
            }
            Dim rbMove As New RadioButton() With {.Text = "Move them to:", .AutoSize = True, .Checked = True, .Location = New Point(12, 46)}
            Dim cboTarget As New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(130, 44), .Width = 258}
            cboTarget.Items.AddRange(others.Select(Function(p) CObj(p.Text)).ToArray())
            cboTarget.SelectedIndex = 0
            Dim rbDelete As New RadioButton() With {.Text = "Delete the links too", .AutoSize = True, .Location = New Point(12, 78)}
            Dim lblBackup As New Label() With {
                .Text = "A copy of the current layout is saved to BACKUP-XML first.",
                .AutoSize = True, .Location = New Point(12, 108)
            }
            AddHandler rbMove.CheckedChanged, Sub() cboTarget.Enabled = rbMove.Checked

            Dim btnOK As New Button() With {
                .Text = "Delete Tab", .DialogResult = DialogResult.OK,
                .Size = New Size(96, 30), .Location = New Point(206, 136)
            }
            Dim btnCancel As New Button() With {
                .Text = "Cancel", .DialogResult = DialogResult.Cancel,
                .Size = New Size(80, 30), .Location = New Point(308, 136)
            }

            ' No AcceptButton: Enter shouldn't delete anything.
            dlg.Controls.AddRange({lbl, rbMove, cboTarget, rbDelete, lblBackup, btnOK, btnCancel})
            dlg.CancelButton = btnCancel
            StyleDialog(dlg)
            dlg.ActiveControl = btnCancel

            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return False

            If rbMove.Checked Then
                moveTarget = others(cboTarget.SelectedIndex)
                Return True
            End If

            moveTarget = Nothing
            Return MessageBox.Show($"Permanently remove the {linkCount} link{If(linkCount = 1, "", "s")} on ""{page.Text}""?" & vbCrLf & vbCrLf &
                                   "The files and folders themselves are not touched.",
                                   "Delete Tab", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                                   MessageBoxDefaultButton.Button2) = DialogResult.Yes
        End Using
    End Function

    ''' <summary>Applies the dark theme to a runtime-built dialog (no-op in light mode).</summary>
    Private Sub StyleDialog(dlg As Form)
        If Not isDarkMode Then Return
        dlg.BackColor = DarkFormBack
        For Each ctrl As Control In dlg.Controls
            StyleControlRecursive(ctrl, DarkFormBack, DarkControlBack, DarkForeColor)
        Next
    End Sub

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
                nameLabel.Text = "(H) " & DisplayNameOf(iconBox)
            End If
        Else
            iconBox.BackColor = Color.Transparent
            If nameLabel IsNot Nothing Then
                nameLabel.ForeColor = If(isDarkMode, DarkForeColor, LightForeColor)
                nameLabel.Text = DisplayNameOf(iconBox)
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
        ' The layout file couldn't be read at startup; don't replace it with this session's partial view.
        If layoutLoadFailed OrElse isReloadingLayout Then Return

        ' There is always at least one grid tab; a layout with none would wipe every link.
        If GridTabPages().Count = 0 Then Return

        Try
            Dim doc As New XmlDocument()
            Dim root As XmlElement = doc.CreateElement("ProgramManagerLayout")
            doc.AppendChild(root)

            ' Save theme preference and the chosen screenshot folder (if any).
            Dim settingsElement As XmlElement = doc.CreateElement("Settings")
            settingsElement.SetAttribute("darkMode", isDarkMode.ToString())
            If Not String.IsNullOrEmpty(screenshotFolderOverride) Then
                settingsElement.SetAttribute("screenshotFolder", screenshotFolderOverride)
            End If
            root.AppendChild(settingsElement)

            Dim tabsElement As XmlElement = doc.CreateElement("Tabs")
            root.AppendChild(tabsElement)

            Dim pages As List(Of TabPage) = GridTabPages()
            For tabIndex As Integer = 0 To pages.Count - 1
                Dim tlp As TableLayoutPanel = GridOf(pages(tabIndex))

                Dim tabElement As XmlElement = doc.CreateElement("Tab")
                tabElement.SetAttribute("index", tabIndex.ToString())
                tabElement.SetAttribute("name", pages(tabIndex).Text)
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
                                    Dim savedName As String = ""
                                    If iconNames.TryGetValue(pb, savedName) AndAlso Not String.IsNullOrEmpty(savedName) Then
                                        iconEl.SetAttribute("name", savedName)
                                    End If
                                    tabElement.AppendChild(iconEl)
                                End If
                            Next
                        End If
                    Next
                Next
            Next

            ' Write to a temp file first, then swap it in, so an interrupted save
            ' (crash, power loss, USB pulled) can't leave a half-written layout behind.
            Dim tempPath As String = LayoutFilePath & ".tmp"
            doc.Save(tempPath)
            IO.File.Move(tempPath, LayoutFilePath, True)

        Catch ex As Exception
            MessageBox.Show("Could not save layout: " & ex.Message,
                            "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    ' --- Load Layout from XML ---

    ''' <summary>
    ''' Rebuilds the grid tabs from the layout XML (one tab per saved &lt;Tab&gt;), then
    ''' places the icons. Links to missing files or folders still get their cell (with the
    ''' warning icon), so a link to an unplugged USB drive survives until the user removes it.
    ''' </summary>
    Private Sub LoadLayout()
        If IO.File.Exists(LayoutFilePath) Then
            Try
                Dim doc As New XmlDocument()
                doc.Load(LayoutFilePath)

                ' Load theme preference.
                Dim settingsNode As XmlNode = doc.SelectSingleNode("//Settings")
                If settingsNode IsNot Nothing Then
                    Dim darkAttr As XmlAttribute = settingsNode.Attributes("darkMode")
                    If darkAttr IsNot Nothing Then
                        isDarkMode = Boolean.Parse(darkAttr.Value)
                    End If

                    ' Restore the chosen screenshot folder; falls back to default if gone.
                    Dim folderAttr As XmlAttribute = settingsNode.Attributes("screenshotFolder")
                    If folderAttr IsNot Nothing Then
                        SetScreenshotFolder(folderAttr.Value)
                    End If
                End If

                ' Build the tabs first; icons are placed after the theme is applied
                ' (the same order the fixed-tab version used).
                Dim pending As New List(Of KeyValuePair(Of TableLayoutPanel, XmlNodeList))()

                ' Try new tabbed format first.
                Dim tabNodes As XmlNodeList = doc.SelectNodes("//Tabs/Tab")
                If tabNodes IsNot Nothing AndAlso tabNodes.Count > 0 Then
                    ' One grid tab per saved <Tab>, in saved index order.
                    Dim ordered = tabNodes.Cast(Of XmlNode)().
                        OrderBy(Function(n) Integer.Parse(n.Attributes("index").Value))
                    For Each tabNode As XmlNode In ordered
                        Dim tabName As String = tabNode.Attributes("name")?.Value
                        If String.IsNullOrEmpty(tabName) Then tabName = "Tab " & (GridTabPages().Count + 1)
                        Dim page As TabPage = AddGridTab(tabName)
                        pending.Add(New KeyValuePair(Of TableLayoutPanel, XmlNodeList)(GridOf(page), tabNode.SelectNodes("Icon")))
                    Next
                Else
                    ' Fall back to legacy single-grid format (default tabs, icons on the first).
                    CreateDefaultTabs()
                    pending.Add(New KeyValuePair(Of TableLayoutPanel, XmlNodeList)(GridOf(GridTabPages()(0)), doc.SelectNodes("//Icons/Icon")))
                End If

                ApplyTheme()

                For Each entry In pending
                    PlaceSavedIcons(entry.Key, entry.Value)
                Next

            Catch ex As Exception
                layoutLoadFailed = True
                ApplyTheme()
                MessageBox.Show("Could not load layout: " & ex.Message & vbCrLf & vbCrLf &
                                "To protect your links, nothing will be saved this session and " &
                                "ProgramManagerLayout.xml has been left untouched. Use Restore from Backup... " &
                                "on the All Links tab to reload one of the copies in BACKUP-XML.",
                                "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End If

        ' First run (or an unreadable file with no tabs in it): start with the default tab set.
        If GridTabPages().Count = 0 Then
            CreateDefaultTabs()
            ApplyTheme()
        End If
    End Sub

    ''' <summary>
    ''' Places a tab's saved icons into its grid. An icon whose saved cell is out of range or
    ''' already taken goes into the first free cell instead, so no saved link is dropped.
    ''' </summary>
    Private Sub PlaceSavedIcons(tlp As TableLayoutPanel, iconNodes As XmlNodeList)
        If iconNodes Is Nothing Then Return

        For Each node As XmlNode In iconNodes
            Dim col As Integer = Integer.Parse(node.Attributes("column").Value)
            Dim row As Integer = Integer.Parse(node.Attributes("row").Value)
            Dim filePath As String = node.Attributes("filePath").Value
            Dim note As String = If(node.Attributes("note")?.Value, "")
            Dim customName As String = If(node.Attributes("name")?.Value, "")

            Dim cell As Panel = Nothing
            If col >= 0 AndAlso col < tlp.ColumnCount AndAlso row >= 0 AndAlso row < tlp.RowCount Then
                cell = TryCast(tlp.GetControlFromPosition(col, row), Panel)
            End If
            If cell Is Nothing OrElse cell.Controls.Count > 0 Then cell = FindFirstEmptyCell(tlp)

            ' A full grid would mean losing this link on the next save; treat it as a load failure.
            If cell Is Nothing Then
                Throw New IO.InvalidDataException("No free cell left for " & filePath)
            End If

            PlaceIconInCell(cell, filePath, note, customName)
        Next
    End Sub

End Class
