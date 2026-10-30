# ProgramManager

A Windows desktop launcher written in **VB.NET WinForms** (.NET 10, `net10.0-windows`). The owner is a single developer who codes in VB.NET and tests in Visual Studio.

## Workflow

- Work on the local files on `main`. Don't create branches or pull requests.
- Don't commit or push. `Release.ps1` commits, pushes to `main`, and publishes the GitHub release once the owner has tested a change.
- Before handing a change over, build it the way the release does and fix every error:
  `dotnet build .\ProgramManager.vbproj -c Release`
  (The release build runs WinForms analyzers that can fail where a Visual Studio Debug build did not, e.g. WFO1000 on public properties of a control: mark code-only properties `Browsable(False)` and `DesignerSerializationVisibility(Hidden)`.)
- The owner runs and tests the app in Visual Studio. Say what to try.
- To release: `.\Release.ps1 -Version x.y.z -Message "..."` (a leading "v" in the version is accepted).

## Code

- `frmMain.vb`: almost all behaviour. Grid tabs are built at runtime from `ProgramManagerLayout.xml` (`AddGridTab`); each grid tab keeps its `TableLayoutPanel` in `TabPage.Tag`, and All Links is always the last tab.
- `frmMain.Designer.vb`: designer code for the fixed All Links tab only.
- `ScreenshotMonitor.vb`: global Prt Sc hook that saves captures.
- `ScreensViewer.vb`: full-tab screenshot viewer shown on a grid tab named SCREENS.
- `LinkPreview.vb`: web links and their cached preview pictures.
- `ShellShortcut.vb`, `Create-AppShortcuts.ps1`: Create App Shortcuts.
- User data (`ProgramManagerLayout.xml`, `ProgramManagerSettings.xml`, `BACKUP-XML`, `Links`) lives next to the exe and is never in the repo or a release zip.

Match the existing style: `''' <summary>` doc comments on non-trivial members, short `'` comments explaining why, `AddHandler` wiring in `frmMain_Load`, `MessageBox` for user-facing errors, and dark/light colours applied in `ApplyTheme`.

## Docs to update with a change

- `ChangeLog.md`: a dated `## yyyy-MM-dd` entry with `### Added/Changed/Fixed - Topic` sections.
- `README.md` Features list, and `Help.md` for user-visible behaviour.
- For a new feature, the Features / Getting Started text in the release notes template inside `Release.ps1`.
