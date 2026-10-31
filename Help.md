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
- **Web links** are supported too - see [Web Links](#web-links) below.

---

## Web Links

A cell can hold a web address as well as a file or folder.

- **Drag a link** from a web page, or the **address bar** (the padlock icon), from Chrome, Edge, or Firefox onto a cell. The page title the browser offers becomes the link's name; otherwise the site name (such as `oilprice.com`) is shown.
- You can also type or paste a `https://...` address into the **Path** box of the link editor on the All Links tab.
- Saved Internet Shortcut (`.url`) files dropped from Explorer get a preview picture too.
- Double-clicking a web link opens it in your default browser.

### Preview Pictures

Like a bookmark manager, Program Manager shows a picture for each web link instead of a plain icon:

- It downloads the page in the background and uses the page's **preview image** (the `og:image` or `twitter:image` the site provides for link previews). If there isn't one, it uses the site's own **icon**. Until a picture arrives, the standard Internet Shortcut icon is shown.
- Pictures are saved in the **Links** folder next to the executable (named from the web address), so each one is only downloaded once and still shows when you're offline. Removing a link, or deleting a tab along with its links, deletes the picture unless another link uses the same address.
- Some sites offer no picture, block automated requests, or only use formats Windows can't read (WebP, SVG). Those keep the Internet Shortcut icon, or get a smaller site icon.

Right-click a web link for these extra commands:

- **Refresh Preview** - Downloads the picture again (for example after the site changed it). Says so if nothing could be found.
- **Choose Image...** - Use a picture file of your own (JPG, PNG, GIF, BMP, or ICO).
- **Paste Image** - Use a picture from the clipboard, for example a snip taken with `Win+Shift+S` or one copied with **Copy Image** in your browser.

**Open Containing Folder** isn't shown for web addresses, since they have no folder.

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

- **Open Containing Folder** - Opens Windows Explorer with the file or folder selected (not shown for web addresses).
- **Edit Note...** - Opens a small dialog to add or edit a free-text note for this shortcut (see below).
- **Hide Folder / Unhide Folder** - Only appears for folders located on the desktop. Toggles the Windows Hidden attribute on the folder (see below).
- **Remove** - Removes the shortcut from the grid (saved right away). This does not delete the actual file or folder.
- **Refresh Preview / Choose Image... / Paste Image** - Only appear for web links (see [Web Links](#web-links)).

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

## Screenshots & the SCREENS Tab

### Print Screen Capture

- While Program Manager is running, pressing **Prt Sc** saves a capture of the whole screen (all monitors) as a JPEG, even when another window has the focus.
- Files are named `screens_yyyy-MM-dd_HH-mm-ss.jpg`; a counter is added if two captures land in the same second.
- The title bar briefly flashes green to confirm the save (Windows 11).
- The clipboard is left untouched.

### Choosing the Screenshot Folder

- Captures go to `Pictures\Screens` by default.
- To use another folder, drag it onto the **Screenshots** box on the All Links tab. Click **Default** to go back to `Pictures\Screens`.
- The choice is saved in `ProgramManagerLayout.xml`. If the saved folder no longer exists, the default is used.

### The SCREENS Tab

Name any grid tab **SCREENS** (capitals don't matter) and the whole tab becomes a screenshot viewer:

- It shows the **newest screenshot** in the screenshot folder. If the SCREENS tab holds a link to a folder, that folder is shown instead.
- A new **Prt Sc** capture appears straight away. Pictures saved into the folder by other tools appear too, while you are looking at the newest one.
- **Paging through screenshots:**
  - The **scroll bar** along the bottom runs from the oldest (left) to the newest (right).
  - **Mouse wheel** up shows a newer screenshot, down an older one.
  - After clicking the picture, **Left / Right**, **Page Up / Page Down**, **Home** (oldest) and **End** (newest) also work.
  - The toolbar buttons go to the oldest, older, newer and newest screenshot. The toolbar also shows the position, file name and time.
- **Double-click** the picture, or click **Open in Paint**, to open it in Paint. Paint can save over the screenshot while it is on screen.
- **Open Folder** opens File Explorer with the screenshot selected.
- JPG, PNG, BMP and GIF pictures are shown, sorted by the time they were last saved.

Links you had on the tab are kept, hidden behind the viewer, and still appear on the All Links tab, where you can edit them. Rename the tab to anything else to get its icon grid back.

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

## First Run

The very first time Program Manager starts (when there is no `ProgramManagerLayout.xml` next to the `.exe` yet), it does a one-time setup:

- It asks whether to start Program Manager automatically when you sign in. Choosing **Yes** adds a `Program Manager.lnk` shortcut to your Startup folder (`shell:startup`); delete it from there to undo. The question is skipped if that shortcut already exists.
- In the background, it runs **Create App Shortcuts** and, when that finishes, places a link to `Documents\All Apps Shortcuts` in the first free cell of the first tab.

If the shortcut script fails, a message explains why and you can run it later from the All Links tab. The setup never runs again once a layout file exists, so upgrading or copying an existing folder doesn't trigger it.

---

## Roll-Up (Minimize to Title Bar)

- **Double-click the title bar** to collapse the window down to just the title bar.
- Double-click the title bar again to **restore** the window to its previous size.

---

## Persistence

All settings are saved automatically when the application closes:

- **ProgramManagerLayout.xml** - Stores all shortcut positions, tab names, notes, custom link names, the dark mode preference, and the chosen screenshot folder.
- **ProgramManagerSettings.xml** - Stores window size, position, and state.

Both files are saved in the same directory as the application executable. Preview pictures for web links are kept in the **Links** subfolder.

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
