# Program Manager - Help

## Overview

Program Manager is a lightweight desktop utility for organizing shortcuts to your programs, files, and folders across multiple tabbed pages. Drag and drop items onto a grid, rearrange them, and launch anything with a double-click.

---

## Tabbed Interface

The window contains 5 tabs:

- **Tabs 1-4** - Each holds a 4x4 grid (16 cells) for placing shortcuts to files and folders.
- **Tab 5 ("All Links")** - A management tab for renaming tabs, toggling dark mode, and viewing all shortcuts in one list.

---

## Adding Shortcuts

- **Drag and drop** files or folders from Windows Explorer onto any cell in Tabs 1-4.
- Dropping onto an **empty cell** places the shortcut there.
- Dropping onto the **tab background** (not a specific cell) places the shortcut in the first available empty cell.
- Dropping **multiple files** at once fills cells in order from the first empty cell.
- Both **files and folders** are supported. Folders display the Windows folder icon.

---

## Launching Shortcuts

- **Double-click** any icon in the grid to open the associated file or folder.
- On the **All Links** tab (Tab 5), double-click any row in the list to launch that item.

---

## Rearranging Icons

- **Click and drag** an icon to another cell within the same tab to move it.
- If the target cell already has an icon, the two icons **swap** positions.
- Dragging requires moving the mouse a short distance before the drag begins, so accidental drags during double-clicks are prevented.

---

## Right-Click Context Menu

Right-click any icon to see:

- **Open Containing Folder** - Opens Windows Explorer with the file or folder selected.
- **Hide Folder / Unhide Folder** - Only appears for folders located on the desktop. Toggles the Windows Hidden attribute on the folder (see below).
- **Remove** - Removes the shortcut from the grid. This does not delete the actual file or folder.

---

## Hiding Desktop Folders

For folders that are located on your desktop:

- Right-click the folder icon and select **Hide Folder** to set the Windows Hidden file attribute. The folder will disappear from your desktop (if Windows is configured to hide hidden files).
- The icon in Program Manager shows a visual indicator when the folder is hidden:
  - The icon gets a **grey overlay**
  - The label turns grey and is prefixed with **(H)**
- Right-click and select **Unhide Folder** to make the folder visible on the desktop again.

---

## Tab 5 - All Links & Settings

### Renaming Tabs

- The top section shows a text field for each tab (Tab 1 through Tab 4).
- Type a new name and it **updates the tab header immediately** as you type.
- Click the **Apply** button to save the names to disk right away.
- Tab names are also saved automatically when the application closes.

### All Links List

- Below the settings area is a list view showing every shortcut across all four tabs.
- Columns: **Tab** (which tab it's on), **Name** (file/folder name), **Path** (full path).
- The list **refreshes automatically** each time you switch to Tab 5.
- **Double-click** any row to launch that file or folder.

---

## Dark / Light Mode

- On Tab 5, click the **"Switch to Dark Mode"** button to toggle the theme.
- Dark mode applies a dark background to the form, all tab pages, grid cells, and Tab 5 controls.
- The theme preference is **saved automatically** and restored on next launch.

---

## Roll-Up (Minimize to Title Bar)

- **Double-click the title bar** to collapse the window down to just the title bar.
- Double-click the title bar again to **restore** the window to its previous size.

---

## Persistence

All settings are saved automatically when the application closes:

- **ProgramManagerLayout.xml** - Stores all shortcut positions, tab names, and the dark mode preference.
- **ProgramManagerSettings.xml** - Stores window size, position, and state.

Both files are saved in the same directory as the application executable.

---

## Grid Capacity

Each tab has a 4x4 grid providing **16 cells per tab** and **64 cells total** across all four tabs. If a tab's grid is full, you will be prompted to remove an icon before adding more.
