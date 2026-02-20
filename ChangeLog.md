# Changelog

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
