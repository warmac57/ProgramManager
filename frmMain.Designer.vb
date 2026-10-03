<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmMain
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        TabControl1 = New TabControl()
        tabAllLinks = New TabPage()
        Tab5MainPanel = New Panel()
        lvwAllLinks = New ListView()
        colTab = New ColumnHeader()
        colName = New ColumnHeader()
        colPath = New ColumnHeader()
        colNote = New ColumnHeader()
        Tab5SettingsPanel = New Panel()
        Tab5SettingsTable = New TableLayoutPanel()
        btnRestoreBackup = New Button()
        pnlLinkEditor = New Panel()
        tlpLinkEditor = New TableLayoutPanel()
        lblLinkTab = New Label()
        cboLinkTab = New ComboBox()
        lblLinkName = New Label()
        txtLinkName = New TextBox()
        lblLinkPath = New Label()
        txtLinkPath = New TextBox()
        flpLinkBrowse = New FlowLayoutPanel()
        btnLinkBrowseFile = New Button()
        btnLinkBrowseFolder = New Button()
        lblLinkNote = New Label()
        txtLinkNote = New TextBox()
        lblLinkStatus = New Label()
        flpLinkButtons = New FlowLayoutPanel()
        btnLinkSave = New Button()
        btnLinkRevert = New Button()
        btnLinkRemove = New Button()
        btnLinkOpenFolder = New Button()
        btnLinkLaunch = New Button()
        pnlTabManager = New Panel()
        lvwTabs = New ListView()
        colTabName = New ColumnHeader()
        colTabLinks = New ColumnHeader()
        flpTabButtons = New FlowLayoutPanel()
        btnTabAdd = New Button()
        btnTabRename = New Button()
        btnTabMoveUp = New Button()
        btnTabMoveDown = New Button()
        btnTabDelete = New Button()
        lblTheme = New Label()
        btnDarkModeToggle = New Button()
        btnResetScreenshotFolder = New Button()
        Label2 = New Label()
        txtScreenshotFolder = New TextBox()
        Label1 = New Label()
        TabControl1.SuspendLayout()
        tabAllLinks.SuspendLayout()
        Tab5MainPanel.SuspendLayout()
        pnlLinkEditor.SuspendLayout()
        tlpLinkEditor.SuspendLayout()
        flpLinkBrowse.SuspendLayout()
        flpLinkButtons.SuspendLayout()
        Tab5SettingsPanel.SuspendLayout()
        Tab5SettingsTable.SuspendLayout()
        pnlTabManager.SuspendLayout()
        flpTabButtons.SuspendLayout()
        SuspendLayout()
        ' 
        ' TabControl1
        ' 
        TabControl1.Controls.Add(tabAllLinks)
        TabControl1.Dock = DockStyle.Fill
        TabControl1.DrawMode = TabDrawMode.OwnerDrawFixed
        TabControl1.Multiline = True
        TabControl1.ItemSize = New Size(48, 30)
        TabControl1.Location = New Point(0, 0)
        TabControl1.Name = "TabControl1"
        TabControl1.SelectedIndex = 0
        TabControl1.Size = New Size(672, 532)
        TabControl1.TabIndex = 0
        ' 
        ' tabAllLinks
        ' 
        tabAllLinks.Controls.Add(Tab5MainPanel)
        tabAllLinks.Location = New Point(4, 34)
        tabAllLinks.Name = "tabAllLinks"
        tabAllLinks.Padding = New Padding(3)
        tabAllLinks.Size = New Size(664, 494)
        tabAllLinks.TabIndex = 10
        tabAllLinks.Text = "All Links"
        tabAllLinks.UseVisualStyleBackColor = True
        ' 
        ' Tab5MainPanel
        ' 
        Tab5MainPanel.AutoScroll = True
        Tab5MainPanel.Controls.Add(lvwAllLinks)
        Tab5MainPanel.Controls.Add(pnlLinkEditor)
        Tab5MainPanel.Controls.Add(Tab5SettingsPanel)
        Tab5MainPanel.Dock = DockStyle.Fill
        Tab5MainPanel.Location = New Point(3, 3)
        Tab5MainPanel.Name = "Tab5MainPanel"
        Tab5MainPanel.Size = New Size(658, 488)
        Tab5MainPanel.TabIndex = 0
        ' 
        ' lvwAllLinks
        ' 
        lvwAllLinks.Columns.AddRange(New ColumnHeader() {colTab, colName, colPath, colNote})
        lvwAllLinks.Dock = DockStyle.Fill
        lvwAllLinks.FullRowSelect = True
        lvwAllLinks.GridLines = True
        lvwAllLinks.HideSelection = False
        lvwAllLinks.MultiSelect = False
        lvwAllLinks.Location = New Point(0, 247)
        lvwAllLinks.Name = "lvwAllLinks"
        lvwAllLinks.Size = New Size(658, 241)
        lvwAllLinks.TabIndex = 0
        lvwAllLinks.UseCompatibleStateImageBehavior = False
        lvwAllLinks.View = View.Details
        ' 
        ' colTab
        ' 
        colTab.Text = "Tab"
        colTab.Width = 80
        ' 
        ' colName
        ' 
        colName.Text = "Name"
        colName.Width = 200
        ' 
        ' colPath
        ' 
        colPath.Text = "Path"
        colPath.Width = 300
        ' 
        ' colNote
        ' 
        colNote.Text = "Note"
        colNote.Width = 200
        ' 
        ' pnlLinkEditor
        ' 
        pnlLinkEditor.Controls.Add(tlpLinkEditor)
        pnlLinkEditor.Dock = DockStyle.Bottom
        pnlLinkEditor.Name = "pnlLinkEditor"
        pnlLinkEditor.Padding = New Padding(10, 6, 10, 6)
        pnlLinkEditor.Size = New Size(658, 168)
        pnlLinkEditor.TabIndex = 2
        ' 
        ' tlpLinkEditor
        ' 
        tlpLinkEditor.ColumnCount = 5
        tlpLinkEditor.ColumnStyles.Add(New ColumnStyle())
        tlpLinkEditor.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 40F))
        tlpLinkEditor.ColumnStyles.Add(New ColumnStyle())
        tlpLinkEditor.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 60F))
        tlpLinkEditor.ColumnStyles.Add(New ColumnStyle())
        tlpLinkEditor.Controls.Add(lblLinkTab, 0, 0)
        tlpLinkEditor.Controls.Add(cboLinkTab, 1, 0)
        tlpLinkEditor.Controls.Add(lblLinkName, 2, 0)
        tlpLinkEditor.Controls.Add(txtLinkName, 3, 0)
        tlpLinkEditor.Controls.Add(lblLinkPath, 0, 1)
        tlpLinkEditor.Controls.Add(txtLinkPath, 1, 1)
        tlpLinkEditor.Controls.Add(flpLinkBrowse, 4, 1)
        tlpLinkEditor.Controls.Add(lblLinkNote, 0, 2)
        tlpLinkEditor.Controls.Add(txtLinkNote, 1, 2)
        tlpLinkEditor.Controls.Add(lblLinkStatus, 0, 3)
        tlpLinkEditor.Controls.Add(flpLinkButtons, 2, 3)
        tlpLinkEditor.SetColumnSpan(txtLinkName, 2)
        tlpLinkEditor.SetColumnSpan(txtLinkPath, 3)
        tlpLinkEditor.SetColumnSpan(txtLinkNote, 4)
        tlpLinkEditor.SetColumnSpan(lblLinkStatus, 2)
        tlpLinkEditor.SetColumnSpan(flpLinkButtons, 3)
        tlpLinkEditor.Dock = DockStyle.Fill
        tlpLinkEditor.Name = "tlpLinkEditor"
        tlpLinkEditor.RowCount = 4
        tlpLinkEditor.RowStyles.Add(New RowStyle(SizeType.Absolute, 30F))
        tlpLinkEditor.RowStyles.Add(New RowStyle(SizeType.Absolute, 32F))
        tlpLinkEditor.RowStyles.Add(New RowStyle(SizeType.Percent, 100F))
        tlpLinkEditor.RowStyles.Add(New RowStyle(SizeType.Absolute, 34F))
        tlpLinkEditor.Size = New Size(638, 156)
        tlpLinkEditor.TabIndex = 0
        ' 
        ' lblLinkTab
        ' 
        lblLinkTab.Anchor = AnchorStyles.Left
        lblLinkTab.AutoSize = True
        lblLinkTab.Name = "lblLinkTab"
        lblLinkTab.TabIndex = 0
        lblLinkTab.Text = "Tab:"
        ' 
        ' cboLinkTab
        ' 
        cboLinkTab.Dock = DockStyle.Fill
        cboLinkTab.DropDownStyle = ComboBoxStyle.DropDownList
        cboLinkTab.Name = "cboLinkTab"
        cboLinkTab.TabIndex = 1
        ' 
        ' lblLinkName
        ' 
        lblLinkName.Anchor = AnchorStyles.Left
        lblLinkName.AutoSize = True
        lblLinkName.Name = "lblLinkName"
        lblLinkName.TabIndex = 2
        lblLinkName.Text = "Name:"
        ' 
        ' txtLinkName
        ' 
        txtLinkName.Dock = DockStyle.Fill
        txtLinkName.Name = "txtLinkName"
        txtLinkName.PlaceholderText = "(file or folder name)"
        txtLinkName.TabIndex = 3
        ' 
        ' lblLinkPath
        ' 
        lblLinkPath.Anchor = AnchorStyles.Left
        lblLinkPath.AutoSize = True
        lblLinkPath.Name = "lblLinkPath"
        lblLinkPath.TabIndex = 4
        lblLinkPath.Text = "Path:"
        ' 
        ' txtLinkPath
        ' 
        txtLinkPath.AllowDrop = True
        txtLinkPath.Dock = DockStyle.Fill
        txtLinkPath.Margin = New Padding(3, 4, 3, 3)
        txtLinkPath.Name = "txtLinkPath"
        txtLinkPath.TabIndex = 5
        ' 
        ' flpLinkBrowse
        ' 
        flpLinkBrowse.AutoSize = True
        flpLinkBrowse.Controls.Add(btnLinkBrowseFile)
        flpLinkBrowse.Controls.Add(btnLinkBrowseFolder)
        flpLinkBrowse.Margin = New Padding(0)
        flpLinkBrowse.Name = "flpLinkBrowse"
        flpLinkBrowse.TabIndex = 6
        flpLinkBrowse.WrapContents = False
        ' 
        ' btnLinkBrowseFile
        ' 
        btnLinkBrowseFile.Margin = New Padding(3)
        btnLinkBrowseFile.Name = "btnLinkBrowseFile"
        btnLinkBrowseFile.Size = New Size(70, 26)
        btnLinkBrowseFile.TabIndex = 0
        btnLinkBrowseFile.Text = "File..."
        btnLinkBrowseFile.UseVisualStyleBackColor = True
        ' 
        ' btnLinkBrowseFolder
        ' 
        btnLinkBrowseFolder.Margin = New Padding(3)
        btnLinkBrowseFolder.Name = "btnLinkBrowseFolder"
        btnLinkBrowseFolder.Size = New Size(70, 26)
        btnLinkBrowseFolder.TabIndex = 1
        btnLinkBrowseFolder.Text = "Folder..."
        btnLinkBrowseFolder.UseVisualStyleBackColor = True
        ' 
        ' lblLinkNote
        ' 
        lblLinkNote.Anchor = AnchorStyles.Left
        lblLinkNote.AutoSize = True
        lblLinkNote.Name = "lblLinkNote"
        lblLinkNote.TabIndex = 7
        lblLinkNote.Text = "Note:"
        lblLinkNote.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        lblLinkNote.Margin = New Padding(3, 6, 3, 0)
        ' 
        ' txtLinkNote
        ' 
        txtLinkNote.Dock = DockStyle.Fill
        txtLinkNote.Multiline = True
        txtLinkNote.Name = "txtLinkNote"
        txtLinkNote.ScrollBars = ScrollBars.Vertical
        txtLinkNote.TabIndex = 8
        ' 
        ' lblLinkStatus
        ' 
        lblLinkStatus.AutoEllipsis = True
        lblLinkStatus.Dock = DockStyle.Fill
        lblLinkStatus.Name = "lblLinkStatus"
        lblLinkStatus.TabIndex = 9
        lblLinkStatus.Text = "Select a link above to edit it."
        lblLinkStatus.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' flpLinkButtons
        ' 
        flpLinkButtons.Controls.Add(btnLinkSave)
        flpLinkButtons.Controls.Add(btnLinkRevert)
        flpLinkButtons.Controls.Add(btnLinkRemove)
        flpLinkButtons.Controls.Add(btnLinkOpenFolder)
        flpLinkButtons.Controls.Add(btnLinkLaunch)
        flpLinkButtons.Dock = DockStyle.Fill
        flpLinkButtons.FlowDirection = FlowDirection.RightToLeft
        flpLinkButtons.Margin = New Padding(0)
        flpLinkButtons.Name = "flpLinkButtons"
        flpLinkButtons.TabIndex = 10
        flpLinkButtons.WrapContents = False
        ' 
        ' btnLinkSave
        ' 
        btnLinkSave.Margin = New Padding(3)
        btnLinkSave.Name = "btnLinkSave"
        btnLinkSave.Size = New Size(80, 26)
        btnLinkSave.TabIndex = 0
        btnLinkSave.Text = "Save"
        btnLinkSave.UseVisualStyleBackColor = True
        ' 
        ' btnLinkRevert
        ' 
        btnLinkRevert.Margin = New Padding(3)
        btnLinkRevert.Name = "btnLinkRevert"
        btnLinkRevert.Size = New Size(80, 26)
        btnLinkRevert.TabIndex = 1
        btnLinkRevert.Text = "Revert"
        btnLinkRevert.UseVisualStyleBackColor = True
        ' 
        ' btnLinkRemove
        ' 
        btnLinkRemove.Margin = New Padding(3)
        btnLinkRemove.Name = "btnLinkRemove"
        btnLinkRemove.Size = New Size(86, 26)
        btnLinkRemove.TabIndex = 2
        btnLinkRemove.Text = "Remove..."
        btnLinkRemove.UseVisualStyleBackColor = True
        ' 
        ' btnLinkOpenFolder
        ' 
        btnLinkOpenFolder.Margin = New Padding(3)
        btnLinkOpenFolder.Name = "btnLinkOpenFolder"
        btnLinkOpenFolder.Size = New Size(96, 26)
        btnLinkOpenFolder.TabIndex = 3
        btnLinkOpenFolder.Text = "Open Folder"
        btnLinkOpenFolder.UseVisualStyleBackColor = True
        ' 
        ' btnLinkLaunch
        ' 
        btnLinkLaunch.Margin = New Padding(3)
        btnLinkLaunch.Name = "btnLinkLaunch"
        btnLinkLaunch.Size = New Size(80, 26)
        btnLinkLaunch.TabIndex = 4
        btnLinkLaunch.Text = "Launch"
        btnLinkLaunch.UseVisualStyleBackColor = True
        ' 
        ' Tab5SettingsPanel
        ' 
        Tab5SettingsPanel.Controls.Add(Tab5SettingsTable)
        Tab5SettingsPanel.Dock = DockStyle.Top
        Tab5SettingsPanel.Location = New Point(0, 0)
        Tab5SettingsPanel.Name = "Tab5SettingsPanel"
        Tab5SettingsPanel.Padding = New Padding(10)
        Tab5SettingsPanel.Size = New Size(658, 247)
        Tab5SettingsPanel.TabIndex = 1
        ' 
        ' Tab5SettingsTable
        ' 
        Tab5SettingsTable.ColumnCount = 4
        Tab5SettingsTable.ColumnStyles.Add(New ColumnStyle())
        Tab5SettingsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        Tab5SettingsTable.ColumnStyles.Add(New ColumnStyle())
        Tab5SettingsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50F))
        Tab5SettingsTable.Controls.Add(pnlTabManager, 0, 0)
        Tab5SettingsTable.Controls.Add(lblTheme, 0, 6)
        Tab5SettingsTable.Controls.Add(btnDarkModeToggle, 1, 6)
        Tab5SettingsTable.Controls.Add(btnResetScreenshotFolder, 2, 7)
        Tab5SettingsTable.Controls.Add(Label2, 0, 7)
        Tab5SettingsTable.Controls.Add(txtScreenshotFolder, 1, 7)
        Tab5SettingsTable.Controls.Add(Label1, 3, 7)
        Tab5SettingsTable.SetColumnSpan(pnlTabManager, 4)
        Tab5SettingsTable.SetRowSpan(pnlTabManager, 6)
        Tab5SettingsTable.Dock = DockStyle.Fill
        Tab5SettingsTable.Location = New Point(10, 10)
        Tab5SettingsTable.Name = "Tab5SettingsTable"
        Tab5SettingsTable.RowCount = 8
        Tab5SettingsTable.RowStyles.Add(New RowStyle(SizeType.Absolute, 28F))
        Tab5SettingsTable.RowStyles.Add(New RowStyle(SizeType.Absolute, 28F))
        Tab5SettingsTable.RowStyles.Add(New RowStyle(SizeType.Absolute, 28F))
        Tab5SettingsTable.RowStyles.Add(New RowStyle(SizeType.Absolute, 28F))
        Tab5SettingsTable.RowStyles.Add(New RowStyle(SizeType.Absolute, 28F))
        Tab5SettingsTable.RowStyles.Add(New RowStyle(SizeType.Absolute, 28F))
        Tab5SettingsTable.RowStyles.Add(New RowStyle(SizeType.Absolute, 28F))
        Tab5SettingsTable.RowStyles.Add(New RowStyle(SizeType.Absolute, 28F))
        Tab5SettingsTable.RowStyles.Add(New RowStyle(SizeType.Absolute, 20F))
        Tab5SettingsTable.Size = New Size(638, 227)
        Tab5SettingsTable.TabIndex = 0
        ' 
        ' pnlTabManager
        ' 
        pnlTabManager.Controls.Add(lvwTabs)
        pnlTabManager.Controls.Add(flpTabButtons)
        pnlTabManager.Dock = DockStyle.Fill
        pnlTabManager.Margin = New Padding(3, 2, 3, 2)
        pnlTabManager.Name = "pnlTabManager"
        pnlTabManager.Size = New Size(632, 164)
        pnlTabManager.TabIndex = 29
        ' 
        ' lvwTabs
        ' 
        lvwTabs.Columns.AddRange(New ColumnHeader() {colTabName, colTabLinks})
        lvwTabs.Dock = DockStyle.Fill
        lvwTabs.FullRowSelect = True
        lvwTabs.GridLines = True
        lvwTabs.HeaderStyle = ColumnHeaderStyle.Nonclickable
        lvwTabs.HideSelection = False
        lvwTabs.LabelEdit = True
        lvwTabs.MultiSelect = False
        lvwTabs.Name = "lvwTabs"
        lvwTabs.Size = New Size(513, 164)
        lvwTabs.TabIndex = 0
        lvwTabs.UseCompatibleStateImageBehavior = False
        lvwTabs.View = View.Details
        ' 
        ' colTabName
        ' 
        colTabName.Text = "Tab"
        colTabName.Width = 300
        ' 
        ' colTabLinks
        ' 
        colTabLinks.Text = "Links"
        colTabLinks.TextAlign = HorizontalAlignment.Right
        colTabLinks.Width = 60
        ' 
        ' flpTabButtons
        ' 
        flpTabButtons.Controls.Add(btnTabAdd)
        flpTabButtons.Controls.Add(btnTabRename)
        flpTabButtons.Controls.Add(btnTabMoveUp)
        flpTabButtons.Controls.Add(btnTabMoveDown)
        flpTabButtons.Controls.Add(btnTabDelete)
        flpTabButtons.Controls.Add(btnRestoreBackup)
        flpTabButtons.Dock = DockStyle.Right
        flpTabButtons.FlowDirection = FlowDirection.TopDown
        flpTabButtons.Name = "flpTabButtons"
        flpTabButtons.Size = New Size(119, 164)
        flpTabButtons.TabIndex = 1
        flpTabButtons.WrapContents = False
        ' 
        ' btnTabAdd
        ' 
        btnTabAdd.Margin = New Padding(6, 1, 3, 1)
        btnTabAdd.Name = "btnTabAdd"
        btnTabAdd.Size = New Size(110, 25)
        btnTabAdd.TabIndex = 0
        btnTabAdd.Text = "Add Tab"
        btnTabAdd.UseVisualStyleBackColor = True
        ' 
        ' btnTabRename
        ' 
        btnTabRename.Margin = New Padding(6, 1, 3, 1)
        btnTabRename.Name = "btnTabRename"
        btnTabRename.Size = New Size(110, 25)
        btnTabRename.TabIndex = 1
        btnTabRename.Text = "Rename"
        btnTabRename.UseVisualStyleBackColor = True
        ' 
        ' btnTabMoveUp
        ' 
        btnTabMoveUp.Margin = New Padding(6, 1, 3, 1)
        btnTabMoveUp.Name = "btnTabMoveUp"
        btnTabMoveUp.Size = New Size(110, 25)
        btnTabMoveUp.TabIndex = 2
        btnTabMoveUp.Text = "Move Up"
        btnTabMoveUp.UseVisualStyleBackColor = True
        ' 
        ' btnTabMoveDown
        ' 
        btnTabMoveDown.Margin = New Padding(6, 1, 3, 1)
        btnTabMoveDown.Name = "btnTabMoveDown"
        btnTabMoveDown.Size = New Size(110, 25)
        btnTabMoveDown.TabIndex = 3
        btnTabMoveDown.Text = "Move Down"
        btnTabMoveDown.UseVisualStyleBackColor = True
        ' 
        ' btnTabDelete
        ' 
        btnTabDelete.Margin = New Padding(6, 1, 3, 1)
        btnTabDelete.Name = "btnTabDelete"
        btnTabDelete.Size = New Size(110, 25)
        btnTabDelete.TabIndex = 4
        btnTabDelete.Text = "Delete Tab..."
        btnTabDelete.UseVisualStyleBackColor = True
        ' 
        ' btnRestoreBackup
        ' 
        btnRestoreBackup.Margin = New Padding(6, 1, 3, 1)
        btnRestoreBackup.Name = "btnRestoreBackup"
        btnRestoreBackup.Size = New Size(110, 25)
        btnRestoreBackup.TabIndex = 5
        btnRestoreBackup.Text = "Restore..."
        btnRestoreBackup.UseVisualStyleBackColor = True
        ' 
        ' lblTheme
        ' 
        lblTheme.AutoSize = True
        lblTheme.Location = New Point(3, 173)
        lblTheme.Margin = New Padding(3, 5, 3, 0)
        lblTheme.Name = "lblTheme"
        lblTheme.Size = New Size(47, 15)
        lblTheme.TabIndex = 10
        lblTheme.Text = "Theme:"
        ' 
        ' btnDarkModeToggle
        ' 
        btnDarkModeToggle.AutoSize = True
        btnDarkModeToggle.Location = New Point(82, 170)
        btnDarkModeToggle.Margin = New Padding(3, 2, 3, 2)
        btnDarkModeToggle.Name = "btnDarkModeToggle"
        btnDarkModeToggle.Size = New Size(127, 24)
        btnDarkModeToggle.TabIndex = 11
        btnDarkModeToggle.Text = "Switch to Dark Mode"
        btnDarkModeToggle.UseVisualStyleBackColor = True
        ' 
        ' btnResetScreenshotFolder
        ' 
        btnResetScreenshotFolder.Anchor = AnchorStyles.None
        btnResetScreenshotFolder.AutoSize = True
        btnResetScreenshotFolder.Location = New Point(322, 199)
        btnResetScreenshotFolder.Margin = New Padding(3, 2, 3, 2)
        btnResetScreenshotFolder.Name = "btnResetScreenshotFolder"
        btnResetScreenshotFolder.Size = New Size(73, 25)
        btnResetScreenshotFolder.TabIndex = 26
        btnResetScreenshotFolder.Text = "Default"
        btnResetScreenshotFolder.UseVisualStyleBackColor = True
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(3, 201)
        Label2.Margin = New Padding(3, 5, 3, 0)
        Label2.Name = "Label2"
        Label2.Size = New Size(73, 15)
        Label2.TabIndex = 28
        Label2.Text = "Screenshots:"
        ' 
        ' txtScreenshotFolder
        ' 
        txtScreenshotFolder.AllowDrop = True
        txtScreenshotFolder.Dock = DockStyle.Fill
        txtScreenshotFolder.Location = New Point(82, 198)
        txtScreenshotFolder.Margin = New Padding(3, 2, 3, 2)
        txtScreenshotFolder.Name = "txtScreenshotFolder"
        txtScreenshotFolder.ReadOnly = True
        txtScreenshotFolder.Size = New Size(231, 23)
        txtScreenshotFolder.TabIndex = 25
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Dock = DockStyle.Fill
        Label1.Location = New Point(404, 196)
        Label1.Name = "Label1"
        Label1.Size = New Size(231, 31)
        Label1.TabIndex = 27
        Label1.Text = "<-Drag folder to set path, or use Default"
        Label1.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' frmMain
        ' 
        AllowDrop = True
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(672, 532)
        Controls.Add(TabControl1)
        FormBorderStyle = FormBorderStyle.SizableToolWindow
        MinimumSize = New Size(398, 293)
        Name = "frmMain"
        StartPosition = FormStartPosition.CenterScreen
        Text = "Program Manager"
        TabControl1.ResumeLayout(False)
        tabAllLinks.ResumeLayout(False)
        flpLinkButtons.ResumeLayout(False)
        flpLinkBrowse.ResumeLayout(False)
        tlpLinkEditor.ResumeLayout(False)
        tlpLinkEditor.PerformLayout()
        pnlLinkEditor.ResumeLayout(False)
        Tab5MainPanel.ResumeLayout(False)
        Tab5SettingsPanel.ResumeLayout(False)
        pnlTabManager.ResumeLayout(False)
        flpTabButtons.ResumeLayout(False)
        Tab5SettingsTable.ResumeLayout(False)
        Tab5SettingsTable.PerformLayout()
        ResumeLayout(False)
    End Sub

    Friend WithEvents TabControl1 As TabControl
    Friend WithEvents tabAllLinks As TabPage
    Friend WithEvents Tab5MainPanel As Panel
    Friend WithEvents Tab5SettingsPanel As Panel
    Friend WithEvents Tab5SettingsTable As TableLayoutPanel
    Friend WithEvents btnRestoreBackup As Button
    Friend WithEvents pnlLinkEditor As Panel
    Friend WithEvents tlpLinkEditor As TableLayoutPanel
    Friend WithEvents lblLinkTab As Label
    Friend WithEvents cboLinkTab As ComboBox
    Friend WithEvents lblLinkName As Label
    Friend WithEvents txtLinkName As TextBox
    Friend WithEvents lblLinkPath As Label
    Friend WithEvents txtLinkPath As TextBox
    Friend WithEvents flpLinkBrowse As FlowLayoutPanel
    Friend WithEvents btnLinkBrowseFile As Button
    Friend WithEvents btnLinkBrowseFolder As Button
    Friend WithEvents lblLinkNote As Label
    Friend WithEvents txtLinkNote As TextBox
    Friend WithEvents lblLinkStatus As Label
    Friend WithEvents flpLinkButtons As FlowLayoutPanel
    Friend WithEvents btnLinkSave As Button
    Friend WithEvents btnLinkRevert As Button
    Friend WithEvents btnLinkRemove As Button
    Friend WithEvents btnLinkOpenFolder As Button
    Friend WithEvents btnLinkLaunch As Button
    Friend WithEvents pnlTabManager As Panel
    Friend WithEvents lvwTabs As ListView
    Friend WithEvents colTabName As ColumnHeader
    Friend WithEvents colTabLinks As ColumnHeader
    Friend WithEvents flpTabButtons As FlowLayoutPanel
    Friend WithEvents btnTabAdd As Button
    Friend WithEvents btnTabRename As Button
    Friend WithEvents btnTabMoveUp As Button
    Friend WithEvents btnTabMoveDown As Button
    Friend WithEvents btnTabDelete As Button
    Friend WithEvents lblTheme As Label
    Friend WithEvents btnDarkModeToggle As Button
    Friend WithEvents lvwAllLinks As ListView
    Friend WithEvents colTab As ColumnHeader
    Friend WithEvents colName As ColumnHeader
    Friend WithEvents colPath As ColumnHeader
    Friend WithEvents colNote As ColumnHeader
    Friend WithEvents txtScreenshotFolder As TextBox
    Friend WithEvents btnResetScreenshotFolder As Button
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label

End Class
