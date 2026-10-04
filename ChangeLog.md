# Changelog

## 2026-10-04 (after v1.1.0)

### Changed - Retargeted to .NET 10
- The project now targets `net10.0-windows` (was `net8.0-windows`). .NET 8 support ends on 2026-11-10; .NET 10 is the current long-term-support release. Users need the **.NET 10.0 Desktop Runtime**.
- Build output moves from `bin\<Config>\net8.0-windows` to `bin\<Config>\net10.0-windows`. `Release.ps1` publishes from the new folder, and its release notes link to the .NET 10 runtime.

### Added - README Download and Run Section
- Steps for installing the runtime, downloading the latest release, unblocking the zip, choosing a writable folder, and getting past SmartScreen, plus how to update without losing settings.
- The old Getting Started section is now **Building from Source** and lists the .NET 10 SDK.

## 2026-10-04 - v1.1.0

### Released - v1.1.0
- Published [v1.1.0](https://github.com/warmac57/ProgramManager/releases/tag/v1.1.0) on GitHub. It includes everything since v1.0.4: dynamic tabs with tab management, two-row tab strip, restore from backup, the All Links link editor, custom link names, and the data-safety changes (see the 2026-10-03 entries below).

### Changed - Release Notes
- Updated the release notes template in `Release.ps1` to match the current app: unlimited tabs, tab management, link editor, Print Screen capture, restore from backup, and missing link detection. The old text still described 8 fixed tabs and the removed tab-name boxes.

### Changed - README
- Replaced the single older screenshot with a **Screenshots** section showing a grid tab, note badges, and the All Links tab. The images are in `docs/images`.

### Removed
- Leftover `Readme - Copy.md` and unused `prg-man .jpg` from the repo root.

## 2026-10-03 (Stage 3)

### Added - Link Editor on the All Links Tab
- Selecting a row in the All Links list loads that link into a new editor under the list: **Tab** (move to the first free cell of another tab; a full tab is refused), **Name** (custom display name), **Path** (type, **File...**, **Folder...**, or drag and drop), and **Note**.
- **Save** and **Revert** are enabled only when something changed. **Remove...** (with confirmation), **Open Folder**, and **Launch** act on the selected link.
- A path that doesn't exist (for example an unplugged USB drive) can be saved after a warning; the link keeps the warning icon and the editor shows it as missing.
- Changing a path rebuilds the icon off-grid first, so a failure leaves the original link in place.
- Moving to another row, leaving All Links, or closing the app with unsaved edits asks Save / Discard / Cancel.
- The All Links list is single-select, rows point at their grid icon, and multi-line notes show as " / " in the list.

### Added - Custom Link Names
- Links can have a custom display name, saved as an optional `name` attribute on `<Icon>`. Blank (or the same as the file name) means "use the file name". The hidden-folder `(H)` label uses the custom name too.

### Changed
- The 10 fixed tab-name boxes and the **Apply** button were removed; tab changes save immediately.
- **Restore from Backup...** is now **Restore...**, under the tab buttons.
- Right-click **Remove** on a grid icon now saves immediately (previously only on close).
- Unhiding a folder in dark mode now uses the dark-theme label color (it used to turn black).

## 2026-10-03

### Added - Tab Management (no more 10-tab limit)
- Grid tabs are now built at runtime from `ProgramManagerLayout.xml` (one per `<Tab>`), so any number of tabs is supported. The XML format is unchanged and existing layouts load as-is.
- The All Links tab has a **tab list** (name and link count) with **Add Tab**, **Rename** (in place, or F2), **Move Up/Down**, and **Delete Tab...** buttons. Double-clicking a tab in the list opens it.
- The same commands are on a **right-click menu on the tab headers**; on the All Links header only Add Tab is offered. A tab added from a grid tab's header is inserted after it and opened.
- **Delete Tab** on a tab with links offers to move them to another tab (default) or delete them, with a second confirmation before anything is removed. Moves are refused when the chosen tab lacks free cells, and the last remaining tab can't be deleted. A `BeforeDelete-` copy of the layout is saved to `BACKUP-XML` first.
- The 10 fixed tab-name boxes are detached from the All Links tab (still in the designer pending cleanup).

### Added - Two-Row Tabs
- `TabControl1.Multiline` is on, so the tab strip wraps onto another row when the tabs don't fit.

### Added - Restore from Backup
- **Restore from Backup...** on the All Links tab picks a layout file (BACKUP-XML by default), validates it, confirms, saves a `BeforeRestore-` copy of the current layout, and reloads every tab.

### Changed - Data Safety
- A one-time `PreDynamicTabs-ProgramManagerLayout.xml` snapshot is kept in BACKUP-XML. `PreDynamicTabs-`, `BeforeDelete-`, and `BeforeRestore-` files are never pruned.
- If the layout file can't be read at startup, nothing is saved that session and the file is left untouched.
- Layout saves are written to a temp file and then swapped in. Saving is skipped while the layout is being reloaded, and a layout with no tabs is never written.
- A saved link whose cell is out of range or already taken goes into the first free cell instead of being dropped; a file whose icon can't be read gets a generic icon instead of being dropped.

### Fixed
- Inserting a tab on a visible window crashed the owner-drawn tab header (Windows asks to paint the new tab before WinForms has added it), which froze the app mid-reload.

### Changed - Rename
- `Form1` renamed to `frmMain` (files, class, startup form).

## 2026-06-09

### Added - User-Selectable Screenshot Folder
- The **All Links** settings tab now has a **Screenshots** drop-box: drag any folder onto it to set where `Prt Sc` captures are saved.
- A **Use Default Folder** button reverts the destination to the default `Pictures\Screens` location, which is also shown (marked "(default)") until a custom folder is chosen.
- The chosen folder is saved to `ProgramManagerLayout.xml` (`screenshotFolder` attribute on `<Settings>`) and restored on launch; if the saved folder no longer exists, it silently falls back to the default.
- Only folders are accepted — dropping a file shows a brief prompt and is ignored.

## 2026-06-08

### Added - Background Print Screen Capture
- While Program Manager is running, pressing the `Prt Sc` key saves a full-screen capture to `Pictures\Screens\`.
- Files are named `screens_yyyy-MM-dd_HH-mm-ss.jpg`; if two captures occur in the same second, a counter (`_2`, `_3`, …) is appended so nothing is overwritten.
- The app captures the screen directly (all monitors / virtual desktop), leaving the clipboard untouched. `Alt+Prt Sc` still saves a full-screen image.
- Capture/encoding runs on a background thread so the low-level keyboard hook returns promptly and is never dropped by Windows.
- Implemented in a dedicated `ScreenshotMonitor` class that installs a global `WH_KEYBOARD_LL` hook on form load and removes it on close, so monitoring only runs while the app is active.

### Added - Save Confirmation Flash
- The title bar briefly flashes green (~0.6 s) after each screenshot is saved, then restores to the default color.
- Uses the Windows 11 DWM caption-color attribute (`DWMWA_CAPTION_COLOR`); on Windows 10 the call is a no-op (no flash, no error).

## 2026-04-23

### Added - Tab 10
- Added a 10th icon grid tab titled "Tab 10" inserted between eMails and the All Links management tab.
- The tab is a full 4×4 icon grid (16 slots), bringing total shortcut capacity to 160.
- The tab name is editable from the All Links settings panel (shown in the right column alongside the eMails name field).
- Existing XML layout data for Tabs 1-9 is fully preserved; Tab 10 starts empty on first launch after the update.

## 2026-04-14

### Changed - Missing Link Handling
- Icons whose file or folder path no longer exists are now preserved in the grid instead of being silently dropped.
- Missing-link icons display a warning icon with a grey overlay and greyed label so they are visually distinct but remain in place.
- Double-clicking a missing-link icon shows an informative message explaining the path is gone and how to remove it.
- The shortcut entry continues to be saved to `ProgramManagerLayout.xml` on every save, so the link is not lost across restarts.
- Right-click → **Remove** works normally and cleans up the missing-link entry completely.

## 2026-03-30

### Added - eMails Tab
- Added a 9th icon grid tab titled "eMails" inserted between Tab 8 and the All Links management tab.
- The eMails tab is a full 4×4 icon grid (16 slots), bringing total shortcut capacity to 144.
- The tab name is editable from the All Links settings panel alongside Tabs 1-8.
- Existing XML layout data for Tabs 1-8 is fully preserved; the eMails tab starts empty on first launch after the update.

## 2026-02-24

### Changed - Note Indicator Badge on Icons
- Icons that have a note now display a small green ✓ badge in the top-right corner of the icon image.
- The badge is drawn via the PictureBox Paint event and repaints immediately when a note is added, edited, or removed.
- Icons without a note are unchanged in appearance.

## 2026-02-21

### Added - GitHub Release Pipeline
- Created `Release.ps1` script to automate the full publish workflow: dotnet publish, zip creation, git commit/push, and GitHub Release in a single command.
- Installed GitHub CLI (`gh`) via winget for programmatic release creation.
- Added `ProgramManager-v*.zip` to `.gitignore` to prevent local release zips from being committed.
- Published v1.0.0 as the initial GitHub Release with release notes covering features, requirements, and installation instructions.

## 2026-02-20

### Added - Icon Notes
- Each icon now supports an optional free-text note stored alongside the shortcut.
- Right-click an icon and choose **Edit Note...** to open a small dialog with a multi-line text field.
- Clearing the note and clicking OK removes it.
- Notes are saved to `ProgramManagerLayout.xml` as a `note` attribute on each `<Icon>` element; files without the attribute load cleanly (backward compatible).
- The **All Links** tab now shows a **Note** column so all notes are visible at a glance.

### Added - XML Backups
- On every app launch the existing `ProgramManagerLayout.xml` and `ProgramManagerSettings.xml` are copied into the `BACKUP-XML` subfolder with a `_YYYYMMDD_HHmmss` timestamp suffix.
- Only the 10 most recent backups per file are kept; older ones are deleted automatically.
- Backup failure is silent and non-critical.

### Changed - Source Control Exclusions
- Added `ProgramManagerLayout.xml`, `ProgramManagerSettings.xml`, and `BACKUP-XML/` to `.gitignore` to prevent local shortcut paths from being committed to the public repository.

## 2026-02-18

### Added - Expanded to 8 Grid Tabs
- Added 4 new icon grid tabs (Tab 5-8), doubling total shortcut capacity to 128 slots.
- All Links tab moved from position 5 to position 9.

### Added - Tab 5-8 Name Editing
- Settings panel on the All Links tab now displays 8 tab name editors in a two-column layout (Tabs 1-4 on the left, Tabs 5-8 on the right).

### Added - Boxed Tab Headers
- Tab headers are now owner-drawn with visible rectangular borders for a cleaner boxed look.
- Selected tab uses a brighter background to stand out.
- Border styling adapts to dark and light themes.

## 2026-02-17

### Added - Tabbed Interface
- Replaced the single 4x4 grid with a `TabControl` containing 5 tabs.
- Tabs 1-4 each contain a 4x4 icon grid (64 total shortcut slots).
- Tab 5 ("All Links") serves as a management and settings tab.

### Added - Tab 5 Management UI
- Tab name editing: four text fields allow renaming Tabs 1-4 with live preview as you type.
- Apply button saves tab names to disk immediately.
- All Links ListView displays every shortcut across all tabs with Tab, Name, and Path columns. Double-click any row to launch it. Refreshes automatically when Tab 5 is selected.

### Added - Folder Shortcut Support
- Folders can now be dragged and dropped onto grid cells just like files.
- Folder icons are extracted using the Windows Shell API (`SHGetFileInfo`).
- Folder labels display the full folder name (files still show name without extension).
- Double-clicking a folder shortcut opens it in Windows Explorer.

### Added - Hide/Unhide Desktop Folders
- Right-click context menu gains a "Hide Folder" / "Unhide Folder" option for folders located on the desktop.
- Toggles the Windows Hidden file attribute on the actual folder.
- Visual indicator on hidden folders: grey overlay on the icon and "(H)" prefix on the label.

### Added - Dark / Light Mode
- Toggle button on Tab 5 switches between dark and light themes.
- Dark mode styles the form, all tab pages, grid cells, and Tab 5 controls.
- Theme preference is saved to XML and restored on launch.

### Changed - XML Layout Format
- New per-tab XML structure: `<Tabs><Tab index="0" name="..."><Icon .../></Tab></Tabs>`.
- Tab names and dark mode preference are stored in the layout file.
- Backward-compatible: old single-grid XML format is loaded into Tab 1 on first migration.

### Changed - Designer Controls
- All structural controls (grid panels, Tab 5 labels, textboxes, buttons, ListView) moved from runtime code into `Form1.Designer.vb` for Visual Studio Form Designer editing.
- Only the 16 cell panels per grid tab remain code-generated (repetitive, event-wired).

### Unchanged
- Form settings save/load (size, position, window state).
- Title bar double-click roll-up/unroll.
- Right-click "Open Containing Folder" and "Remove" menu items.
- Drag-and-drop icon rearrangement (swap) within a tab.
- Portable deployment: all data stored next to the executable.
