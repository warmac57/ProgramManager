# Program Manager

A lightweight Windows desktop utility for organizing shortcuts to programs, files, and folders in a tabbed grid interface. Built with VB.NET and Windows Forms (.NET 8).

## Features

- **Tabbed grid layout** - 4 tabs with 4x4 grids (64 shortcut slots) plus a management tab
- **Drag and drop** - Drop files and folders from Explorer onto any grid cell
- **Icon rearrangement** - Drag icons between cells to swap positions
- **File and folder support** - Shortcuts to both files and folders with proper Windows icons
- **Launch on double-click** - Open any shortcut directly from the grid or the All Links list
- **Tab renaming** - Customize tab names from the management tab with live preview
- **Hide/unhide desktop folders** - Toggle the Windows Hidden attribute on desktop folders via right-click
- **Dark / light mode** - Switch themes from the management tab; preference is persisted
- **Roll-up** - Double-click the title bar to collapse the window to just the title bar
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

1. Drag files or folders from Windows Explorer onto any cell in Tabs 1-4.
2. Double-click an icon to launch it.
3. Drag icons between cells to rearrange.
4. Right-click an icon for options (Open Containing Folder, Hide Folder, Remove).
5. Use Tab 5 to rename tabs, toggle dark mode, and view all shortcuts in a list.

For detailed feature documentation, see [Help.md](Help.md).

## Data Files

All data is stored in the application directory (portable, no registry or AppData):

| File | Contents |
|---|---|
| `ProgramManagerLayout.xml` | Shortcut positions, tab names, dark mode preference |
| `ProgramManagerSettings.xml` | Window size, position, and state |

## Multiple Instances

Copy the entire application folder to a new location to run independent instances. Each copy maintains its own settings and shortcuts.

## Recent Changes

### 2026-02-17

- Added tabbed interface with 4 icon grid tabs and a management tab
- Added folder shortcut support with Windows Shell icon extraction
- Added hide/unhide toggle for desktop folders with visual indicator
- Added dark/light mode theme toggle
- Added Tab 5 management UI (tab renaming, All Links list, theme toggle)
- Migrated all structural controls to the Form Designer for visual editing
- Updated XML format to per-tab structure with backward compatibility

For the full changelog, see [ChangeLog.md](ChangeLog.md).
