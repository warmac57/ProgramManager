''' <summary>
''' Full-tab screenshot viewer shown on the grid tab named "SCREENS". Shows the newest
''' picture in a folder (normally the screenshot folder) and pages back through older ones
''' with the scroll bar, the mouse wheel, the arrow keys or the buttons. Double-clicking
''' the picture opens it in Paint. A FileSystemWatcher keeps the list current, so a new
''' capture appears as soon as it is saved.
''' </summary>
Public Class ScreensViewer
    Inherits UserControl

    ' Picture types the viewer lists (the screenshot monitor itself writes .jpg).
    Private Shared ReadOnly ImageExtensions As New HashSet(Of String)(
        {".jpg", ".jpeg", ".png", ".bmp", ".gif"}, StringComparer.OrdinalIgnoreCase)

    ' --- Controls ---
    Private ReadOnly pnlToolbar As New FlowLayoutPanel()
    Private ReadOnly btnFirst As New Button()
    Private ReadOnly btnPrev As New Button()
    Private ReadOnly btnNext As New Button()
    Private ReadOnly btnLast As New Button()
    Private ReadOnly btnPaint As New Button()
    Private ReadOnly btnOpenFolder As New Button()
    Private ReadOnly lblPosition As New Label()
    Private ReadOnly picScreen As New PictureBox()
    Private ReadOnly scrScreens As New HScrollBar()
    Private ReadOnly lblEmpty As New Label()
    Private ReadOnly tipButtons As New ToolTip()

    ' --- State ---
    Private _folder As String = ""
    Private files As New List(Of String)()     ' oldest first, so the newest is files.Last()
    Private currentIndex As Integer = -1
    Private watcher As IO.FileSystemWatcher
    Private ReadOnly rescanTimer As New Timer() With {.Interval = 500}
    Private settingScroll As Boolean = False   ' True while code moves the scroll bar

    Public Sub New()
        Dock = DockStyle.Fill

        ' Toolbar: paging buttons, position text, then Paint / folder buttons.
        pnlToolbar.Dock = DockStyle.Top
        pnlToolbar.AutoSize = True
        pnlToolbar.WrapContents = True
        pnlToolbar.Padding = New Padding(2)

        ConfigureButton(btnFirst, "|◀", "Oldest screenshot (Home)")
        ConfigureButton(btnPrev, "◀", "Older screenshot (Left arrow / wheel down)")
        ConfigureButton(btnNext, "▶", "Newer screenshot (Right arrow / wheel up)")
        ConfigureButton(btnLast, "▶|", "Newest screenshot (End)")
        ConfigureButton(btnPaint, "Open in Paint", "Open this screenshot in Paint (or double-click it)")
        ConfigureButton(btnOpenFolder, "Open Folder", "Show this screenshot in File Explorer")

        lblPosition.AutoSize = True
        lblPosition.Anchor = AnchorStyles.Left
        lblPosition.Margin = New Padding(8, 8, 8, 0)

        pnlToolbar.Controls.AddRange(New Control() {btnFirst, btnPrev, btnNext, btnLast, lblPosition, btnPaint, btnOpenFolder})

        ' The scroll bar pages the screenshots: left end oldest, right end newest.
        scrScreens.Dock = DockStyle.Bottom
        scrScreens.Height = 20
        scrScreens.SmallChange = 1
        scrScreens.LargeChange = 1

        picScreen.Dock = DockStyle.Fill
        picScreen.SizeMode = PictureBoxSizeMode.Zoom
        picScreen.Cursor = Cursors.Hand

        lblEmpty.Dock = DockStyle.Fill
        lblEmpty.TextAlign = ContentAlignment.MiddleCenter
        lblEmpty.Visible = False

        ' Fill controls go in first so the docked toolbar and scroll bar keep their edges.
        Controls.Add(picScreen)
        Controls.Add(lblEmpty)
        Controls.Add(scrScreens)
        Controls.Add(pnlToolbar)

        AddHandler btnFirst.Click, Sub() ShowIndex(0)
        AddHandler btnPrev.Click, Sub() ShowIndex(currentIndex - 1)
        AddHandler btnNext.Click, Sub() ShowIndex(currentIndex + 1)
        AddHandler btnLast.Click, Sub() ShowIndex(files.Count - 1)
        AddHandler btnPaint.Click, Sub() OpenInPaint()
        AddHandler btnOpenFolder.Click, Sub() OpenInExplorer()
        AddHandler picScreen.DoubleClick, Sub() OpenInPaint()
        AddHandler picScreen.MouseWheel, AddressOf Viewer_MouseWheel
        AddHandler lblEmpty.MouseWheel, AddressOf Viewer_MouseWheel
        AddHandler picScreen.MouseDown, Sub() Focus()   ' so the arrow keys page afterwards
        AddHandler scrScreens.ValueChanged, AddressOf Scroll_ValueChanged
        AddHandler rescanTimer.Tick, AddressOf RescanTimer_Tick

        UpdateControls()
    End Sub

    Private Sub ConfigureButton(btn As Button, text As String, tip As String)
        btn.Text = text
        btn.AutoSize = True
        btn.UseVisualStyleBackColor = True
        btn.Margin = New Padding(2)
        tipButtons.SetToolTip(btn, tip)
    End Sub

    ''' <summary>
    ''' Folder whose pictures are shown. Changing it rescans the folder, shows its newest
    ''' picture, and moves the watcher there. Set from code only, so the designer never saves it.
    ''' </summary>
    <System.ComponentModel.Browsable(False)>
    <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property Folder As String
        Get
            Return _folder
        End Get
        Set(value As String)
            value = If(value, "")
            If String.Equals(value, _folder, StringComparison.OrdinalIgnoreCase) Then Return
            _folder = value
            StartWatching()
            Rescan(jumpToNewest:=True)
        End Set
    End Property

    ''' <summary>Rescans the folder and shows the newest picture (e.g. right after a capture).</summary>
    Public Sub ShowNewest()
        Rescan(jumpToNewest:=True)
    End Sub

    ''' <summary>Applies the light or dark theme colors from frmMain.</summary>
    Public Sub ApplyTheme(backColor As Color, controlBack As Color, foreColor As Color)
        Me.BackColor = backColor
        Me.ForeColor = foreColor
        pnlToolbar.BackColor = backColor
        picScreen.BackColor = backColor
        lblEmpty.BackColor = backColor
        lblEmpty.ForeColor = foreColor
        lblPosition.ForeColor = foreColor
        For Each btn As Button In pnlToolbar.Controls.OfType(Of Button)()
            btn.BackColor = controlBack
            btn.ForeColor = foreColor
        Next
    End Sub

    ' --- Folder scanning and watching ---

    Private Sub StartWatching()
        StopWatching()
        If Not IO.Directory.Exists(_folder) Then Return

        ' The watcher raises its events through this control, which needs a window handle
        ' even while the SCREENS tab has never been opened.
        If Not IsHandleCreated Then CreateHandle()

        Try
            watcher = New IO.FileSystemWatcher(_folder) With {
                .NotifyFilter = IO.NotifyFilters.FileName Or IO.NotifyFilters.LastWrite,
                .IncludeSubdirectories = False,
                .SynchronizingObject = Me
            }
            AddHandler watcher.Created, AddressOf Watcher_Changed
            AddHandler watcher.Deleted, AddressOf Watcher_Changed
            AddHandler watcher.Renamed, AddressOf Watcher_Changed
            AddHandler watcher.Changed, AddressOf Watcher_Changed
            watcher.EnableRaisingEvents = True
        Catch
            ' Without a watcher the list still refreshes on a capture or when the tab is opened.
            StopWatching()
        End Try
    End Sub

    Private Sub StopWatching()
        If watcher IsNot Nothing Then
            watcher.EnableRaisingEvents = False
            watcher.Dispose()
            watcher = Nothing
        End If
    End Sub

    Private Sub Watcher_Changed(sender As Object, e As IO.FileSystemEventArgs)
        ' A new file is often still being written when Created fires, and one save raises
        ' several events; wait for the folder to settle, then rescan once.
        rescanTimer.Stop()
        rescanTimer.Start()
    End Sub

    Private Sub RescanTimer_Tick(sender As Object, e As EventArgs)
        rescanTimer.Stop()
        ' Follow new captures only while the newest one is on screen; otherwise stay put.
        Rescan(jumpToNewest:=currentIndex < 0 OrElse currentIndex = files.Count - 1)
    End Sub

    ''' <summary>
    ''' Re-reads the folder's pictures (oldest first). Shows the newest one, or else keeps
    ''' the picture that was on screen if it still exists.
    ''' </summary>
    Public Sub Rescan(Optional jumpToNewest As Boolean = False)
        Dim shownFile As String = CurrentFile()

        Dim found As New List(Of String)()
        Try
            If IO.Directory.Exists(_folder) Then
                found = New IO.DirectoryInfo(_folder).EnumerateFiles().
                    Where(Function(f) ImageExtensions.Contains(f.Extension)).
                    OrderBy(Function(f) f.LastWriteTimeUtc).
                    ThenBy(Function(f) f.Name, StringComparer.OrdinalIgnoreCase).
                    Select(Function(f) f.FullName).
                    ToList()
            End If
        Catch
            ' Folder unreadable right now (unplugged drive, permissions): show it as empty.
        End Try
        files = found

        Dim newIndex As Integer = files.Count - 1
        If Not jumpToNewest AndAlso shownFile IsNot Nothing Then
            Dim kept As Integer = files.FindIndex(Function(f) String.Equals(f, shownFile, StringComparison.OrdinalIgnoreCase))
            If kept >= 0 Then newIndex = kept
        End If

        currentIndex = -1   ' force ShowIndex to reload, in case the file itself changed
        ShowIndex(newIndex)
    End Sub

    ' --- Showing a picture ---

    Private Sub ShowIndex(index As Integer)
        If files.Count = 0 Then
            currentIndex = -1
            SetPicture(Nothing)
            UpdateControls()
            Return
        End If

        index = Math.Max(0, Math.Min(index, files.Count - 1))
        If index = currentIndex Then Return
        currentIndex = index
        SetPicture(LoadPicture(files(index)))
        UpdateControls()
    End Sub

    ''' <summary>
    ''' Loads a picture into memory and closes the file straight away, so Paint (or the
    ''' screenshot monitor) can still write to it. Returns Nothing if it can't be read yet.
    ''' </summary>
    Private Shared Function LoadPicture(path As String) As Image
        Try
            Using stream As New IO.FileStream(path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite Or IO.FileShare.Delete)
                Using img As Image = Image.FromStream(stream)
                    Return New Bitmap(img)
                End Using
            End Using
        Catch
            Return Nothing
        End Try
    End Function

    Private Sub SetPicture(img As Image)
        Dim old As Image = picScreen.Image
        picScreen.Image = img
        old?.Dispose()
    End Sub

    Private Sub UpdateControls()
        Dim hasFiles As Boolean = files.Count > 0
        btnFirst.Enabled = hasFiles AndAlso currentIndex > 0
        btnPrev.Enabled = btnFirst.Enabled
        btnNext.Enabled = hasFiles AndAlso currentIndex < files.Count - 1
        btnLast.Enabled = btnNext.Enabled
        btnPaint.Enabled = hasFiles
        btnOpenFolder.Enabled = IO.Directory.Exists(_folder)
        scrScreens.Enabled = files.Count > 1

        settingScroll = True
        Try
            scrScreens.Minimum = 0
            scrScreens.Maximum = Math.Max(0, files.Count - 1)
            scrScreens.Value = Math.Max(0, currentIndex)
        Finally
            settingScroll = False
        End Try

        If hasFiles Then
            Dim path As String = files(currentIndex)
            Dim stamp As String = IO.File.GetLastWriteTime(path).ToString("g")
            lblPosition.Text = $"{currentIndex + 1} of {files.Count}  —  {IO.Path.GetFileName(path)}  ({stamp})"
            If picScreen.Image Is Nothing Then
                lblEmpty.Text = "This screenshot couldn't be opened (it may still be saving)."
            End If
        Else
            lblPosition.Text = "No screenshots"
            lblEmpty.Text = If(IO.Directory.Exists(_folder),
                               "No screenshots yet." & vbCrLf & vbCrLf & "Press Prt Sc and the capture will appear here." & vbCrLf & _folder,
                               "Screenshot folder not found:" & vbCrLf & _folder)
        End If

        Dim showPicture As Boolean = hasFiles AndAlso picScreen.Image IsNot Nothing
        picScreen.Visible = showPicture
        lblEmpty.Visible = Not showPicture
    End Sub

    ' --- Paging input ---

    Private Sub Scroll_ValueChanged(sender As Object, e As EventArgs)
        If Not settingScroll Then ShowIndex(scrScreens.Value)
    End Sub

    Private Sub Viewer_MouseWheel(sender As Object, e As MouseEventArgs)
        ' Mark it handled, or Windows also passes the wheel on to this control and pages twice.
        Dim handled As HandledMouseEventArgs = TryCast(e, HandledMouseEventArgs)
        If handled IsNot Nothing Then handled.Handled = True

        ' Wheel up = newer, wheel down = older (one screenshot per notch).
        If e.Delta > 0 Then
            ShowIndex(currentIndex + 1)
        ElseIf e.Delta < 0 Then
            ShowIndex(currentIndex - 1)
        End If
    End Sub

    Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
        MyBase.OnMouseWheel(e)
        Viewer_MouseWheel(Me, e)
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        Select Case keyData
            Case Keys.Left, Keys.PageUp
                ShowIndex(currentIndex - 1)
                Return True
            Case Keys.Right, Keys.PageDown
                ShowIndex(currentIndex + 1)
                Return True
            Case Keys.Home
                ShowIndex(0)
                Return True
            Case Keys.End
                ShowIndex(files.Count - 1)
                Return True
        End Select
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    ' --- Opening the picture ---

    Private Function CurrentFile() As String
        If currentIndex < 0 OrElse currentIndex >= files.Count Then Return Nothing
        Return files(currentIndex)
    End Function

    Private Sub OpenInPaint()
        Dim path As String = CurrentFile()
        If path Is Nothing Then Return

        Try
            Process.Start(New ProcessStartInfo("mspaint.exe", $"""{path}""") With {.UseShellExecute = True})
        Catch ex As Exception
            MessageBox.Show("Could not open the screenshot in Paint." & vbCrLf & vbCrLf &
                            "Path: " & path & vbCrLf & vbCrLf &
                            "System Error: " & ex.Message,
                            "Open in Paint", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OpenInExplorer()
        Dim path As String = CurrentFile()
        Try
            If path IsNot Nothing Then
                ' Open the folder with this screenshot selected.
                Process.Start("explorer.exe", $"/select,""{path}""")
            ElseIf IO.Directory.Exists(_folder) Then
                Process.Start(New ProcessStartInfo(_folder) With {.UseShellExecute = True})
            End If
        Catch ex As Exception
            MessageBox.Show("Could not open the folder: " & ex.Message,
                            "Open Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            StopWatching()
            rescanTimer.Dispose()
            tipButtons.Dispose()
            SetPicture(Nothing)
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class
