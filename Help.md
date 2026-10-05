# Program Manager - Help

## Overview

Program Manager is a lightweight desktop utility for organizing shortcuts to your programs, files, and folders across multiple tabbed pages. Drag and drop items onto a grid, rearrange them, and launch anything with a double-click.

---

## Tabbed Interface

The window holds as many grid tabs as you like, followed by the **All Links** tab:

- **Grid tabs** - Each holds a 4x4 grid (16 cells) for placing shortcuts to files and folders. Add, rename, reorder, and delete them as needed (see **Managing Tabs** below).
- **All Links tab** (always last) - A management tab for managing tabs, toggling dark mode, restoring backups, and viewing all shortcuts in one list.
- When the tabs don't fit across the window, they wrap onto a second row. The row holding the selected tab always moves next to the page (standard Windows behavior).

---

## Adding Shortcuts

- **Drag and drop** files or folders from Windows Explorer onto any cell in a grid tab.
- Dropping onto an **empty cell** places the shortcut there.
- Dropping onto the **tab background** (not a specific cell) places the shortcut in the first available empty cell.
- Dropping **multiple files** at once fills cells in order from the first empty cell.
- Both **files and folders** are supported. Folders display the Windows folder icon.

---

## Launching Shortcuts

- **Double-click** any icon in the grid to open the associated file or folder.
- On the **All Links** tab, double-click any row in the list to launch that item.

---

## Rearranging Icons

- **Click and drag** an icon to another cell within the same tab to move it.
- If the target cell already has an icon, the two icons **swap** positions.
- Dragging requires moving the mouse a short distance before the drag begins, so accidental drags during double-clicks are prevented.

---

## Right-Click Context Menu

Right-click any icon to see:

- **Open Containing Folder** - Opens Windows Explorer with the file or folder selected.
- **Edit Note...** - Opens a small dialog to add or edit a free-text note for this shortcut (see below).
- **Hide Folder / Unhide Folder** - Only appears for folders located on the desktop. Toggles the Windows Hidden attribute on the folder (see below).
- **Remove** - Removes the shortcut from the grid (saved right away). This does not delete the actual file or folder.

---

## Icon Notes

Each shortcut can have an optional free-text note attached to it.

- Right-click an icon and choose **Edit Note...** to open the note dialog.
- Type any text in the multi-line field — version numbers, descriptions, reminders, etc.
- Click **OK** to save, or **Cancel** to discard changes.
- Clearing the text field and clicking OK removes the note entirely.
- Notes are saved in `ProgramManagerLayout.xml` alongside the shortcut path.
- Icons that have a note display a small green **✓** badge in the top-right corner of the icon.
- The note for each shortcut is visible in the **Note** column on the All Links tab.

---

## Hiding Desktop Folders

For folders that are located on your desktop:

- Right-click the folder icon and select **Hide Folder** to set the Windows Hidden file attribute. The folder will disappear from your desktop (if Windows is configured to hide hidden files).
- The icon in Program Manager shows a visual indicator when the folder is hidden:
  - The icon gets a **grey overlay**
  - The label turns grey and is prefixed with **(H)**
- Right-click and select **Unhide Folder** to make the folder visible on the desktop again.

---

## All Links Tab

### Managing Tabs

The top section lists every grid tab with its number of links. Select a tab, then:

- **Add Tab** - Adds an empty tab right after the selected one (or at the end if none is selected). You are asked for a name.
- **Rename** - Edits the name in place; you can also click a selected name or press **F2**. Press Enter to keep it, Esc to cancel.
- **Move Up / Move Down** - Moves the tab one place left or right in the tab strip.
- **Delete Tab...** - If the tab has links, choose to **move them to another tab** (the default) or **delete them too** (asks again before removing anything). A tab can't be deleted if it is the only one, and links are only moved when the chosen tab has enough free cells.
- **Double-click** a tab in the list to open it.
- **Restore...** - Reloads the whole layout from a backup (see **Restoring a Backup**).

The same commands are on the **right-click menu of any tab header** (Add Tab, Rename, Move Left/Right, Delete Tab). Right-clicking the All Links header offers Add Tab only.

Every change is saved immediately. Before a tab is deleted, a copy of the layout is saved to `BACKUP-XML` (see **Automatic Backups**).

### All Links List

- Below the settings area is a list view showing every shortcut across all grid tabs.
- Columns: **Tab** (which tab it's on), **Name** (its custom name, or the file/folder name), **Path** (full path), **Note** (any note you have added; line breaks show as " / ").
- The list **refreshes automatically** each time you switch to the All Links tab.
- **Double-click** any row to launch that file or folder.

### Editing a Link

Select a row in the list and its details appear in the editor underneath:

- **Tab** - Pick another tab to move the link there (it goes into the first free cell). A full tab is refused with a message.
- **Name** - A custom name shown under the icon and in the list. Leave it blank to use the file or folder name.
- **Path** - Type a new path, use **File...** or **Folder...**, or drag a file or folder onto the box. **File...** keeps a picked `.lnk` shortcut as the shortcut itself. A path that doesn't exist right now (for example on an unplugged USB drive) can still be saved after a warning; the link then shows the warning icon until the drive is back.
- **Note** - The same note as **Edit Note...** on the icon's right-click menu.

Buttons:

- **Save** / **Revert** - Enabled once something has changed. Save applies and stores the changes; Revert puts the fields back.
- **Remove...** - Removes the link after a confirmation (the file or folder itself is not touched).
- **Open Folder** / **Launch** - Same as the icon's right-click and double-click.

If you move to another row, leave the All Links tab, or close the app with unsaved changes, you are asked whether to save them (**Yes**), discard them (**No**), or stay where you are (**Cancel**). The line at the bottom left shows when there are unsaved changes, or when the link's file or folder can't be found.

---

## Dark / Light Mode

- On the All Links tab, click the **"Switch to Dark Mode"** button to toggle the theme.
- Dark mode applies a dark background to the form, all tab pages, grid cells, and the All Links tab controls.
- The theme preference is **saved automatically** and restored on next launch.

---

## Creating App Shortcuts

- On the All Links tab, click **Create App Shortcuts...** (next to the theme button) and confirm.
- Program Manager runs the bundled `Create-AppShortcuts.ps1` script, which creates a shortcut to every app in the Windows **All Apps** list, including Microsoft Store apps, in `Documents\All Apps Shortcuts`. Each shortcut shows the app's real icon.
- When it finishes, the folder opens in File Explorer. Drag any of the shortcuts onto a grid cell to add them to Program Manager.
- Running it again replaces the shortcuts in that folder with a fresh set. Other files in the folder are left alone.
- The script must be next to `ProgramManager.exe`; it is included in the release zip and the build output.

---

## Roll-Up (Minimize to Title Bar)

- **Double-click the title bar** to collapse the window down to just the title bar.
- Double-click the title bar again to **restore** the window to its previous size.

---

## Persistence

All settings are saved automatically when the application closes:

- **ProgramManagerLayout.xml** - Stores all shortcut positions, tab names, notes, custom link names, and the dark mode preference.
- **ProgramManagerSettings.xml** - Stores window size, position, and state.

Both files are saved in the same directory as the application executable.

### Automatic Backups

Every time the application starts, it backs up both XML files into the **BACKUP-XML** subfolder:

- Backup filenames include a timestamp: e.g. `ProgramManagerLayout_20260220_143012.xml`
- The **10 most recent backups** per file are kept; older ones are deleted automatically.
- This protects your data in the event of a corrupt or accidental layout change.

These extra copies are also written to BACKUP-XML and are **never deleted automatically**:

- `PreDynamicTabs-ProgramManagerLayout.xml` - your layout as it was before tabs could be added and removed (made once).
- `BeforeDelete-ProgramManagerLayout_<timestamp>.xml` - made just before a tab is deleted.
- `BeforeRestore-ProgramManagerLayout_<timestamp>.xml` - made just before a backup is restored.

### Restoring a Backup

- On the All Links tab, click **Restore...** (under the tab buttons) and pick a file (the BACKUP-XML folder opens by default).
- The file is checked first; one that can't be read is refused and nothing changes.
- After you confirm, the current layout is saved as a `BeforeRestore-` copy, so you can switch back by restoring that file.
- All tabs and links are then reloaded from the backup.
- If the layout file ever fails to load at startup, Program Manager tells you and saves nothing that session, leaving the file untouched for you to restore.

---

## Grid Capacity

Each tab has a 4x4 grid providing **16 cells per tab**. There is no limit on the number of tabs, so add a tab when you need more room. If a tab's grid is full, you will be prompted to remove an icon before adding more.
