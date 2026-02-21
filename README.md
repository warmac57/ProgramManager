# Program Manager

A lightweight Windows desktop utility for organizing shortcuts to programs, files, and folders in a tabbed grid interface. Built with VB.NET and Windows Forms (.NET 8).

## Features

- **Tabbed grid layout** - 8 tabs with 4x4 grids (128 shortcut slots) plus an All Links management tab
- **Drag and drop** - Drop files and folders from Explorer onto any grid cell
- **Icon rearrangement** - Drag icons between cells to swap positions
- **File and folder support** - Shortcuts to both files and folders with proper Windows icons
- **Launch on double-click** - Open any shortcut directly from the grid or the All Links list
- **Icon notes** - Attach a free-text note to any shortcut via right-click; notes are saved to XML and visible in the All Links tab
- **Tab renaming** - Customize all 8 tab names from the management tab with live preview
- **Hide/unhide desktop folders** - Toggle the Windows Hidden attribute on desktop folders via right-click
- **Dark / light mode** - Switch themes from the management tab; preference is persisted
- **Roll-up** - Double-click the title bar to collapse the window to just the title bar
- **Automatic XML backups** - On every launch, both data files are backed up to a `BACKUP-XML` subfolder; the last 10 backups per file are retained
- **Portable** - All settings stored next to the executable; copy the folder for independent instances

## Getting Started

### Requirements

- Windows 10/11
- .NET 8.0 SDK or Runtime

### Build and Run

```
dotnet build
dotnet run
```

### Usage

1. Drag files or folders from Windows Explorer onto any cell in Tabs 1-8.
2. Double-click an icon to launch it.
3. Drag icons between cells to rearrange.
4. Right-click an icon for options (Open Containing Folder, Edit Note, Hide Folder, Remove).
5. Use the All Links tab to rename tabs, toggle dark mode, and view all shortcuts with their notes in a list.

For detailed feature documentation, see [Help.md](Help.md).

## Data Files

All data is stored in the application directory (portable, no registry or AppData):

| File | Contents |
|---|---|
| `ProgramManagerLayout.xml` | Shortcut positions, tab names, icon notes, dark mode preference |
| `ProgramManagerSettings.xml` | Window size, position, and state |
| `BACKUP-XML/` | Timestamped backups of both XML files (last 10 per file) |

> **Note:** The XML data files and `BACKUP-XML/` folder are excluded from source control via `.gitignore` since they contain local file paths.

## Multiple Instances

Copy the entire application folder to a new location to run independent instances. Each copy maintains its own settings and shortcuts.

## Recent Changes

### 2026-02-20

- Added per-icon note field, editable via right-click "Edit Note..." dialog; notes saved in XML and shown in the All Links tab
- Added automatic XML backups to `BACKUP-XML/` on every launch (last 10 kept per file)
- Excluded runtime data files from source control via `.gitignore`

For the full changelog, see [ChangeLog.md](ChangeLog.md).
