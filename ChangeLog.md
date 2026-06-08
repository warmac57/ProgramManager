# Changelog

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
