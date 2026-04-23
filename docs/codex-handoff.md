# Latest update

- Local Windows repo for the current session: `C:\Users\Olga\AKB5`
- Active working branch: `interface`
- Latest pushed UI commit on this branch: `7b7a37d` (`Remove tree icon badges`)
- Current session outcome: product direction was clarified and converted into a concrete multi-phase implementation plan in root `Roadmap.md`

# Current repo state

- `AKB5` remains a WinForms desktop app on `.NET 8`
- JSON remains the source of truth
- Excel workbook `v3` remains the current exchange format
- Windows CI publish is configured for both `icon` and `interface`
- The TreeView rendering work already accepted by the user is now:
  - redraw-artifact fix kept in place
  - safe text/icon spacing in owner-draw kept in place
  - custom chevron kept as the only expandability marker
  - semantic `20x20` icons for department/system/panel/device
  - `ItemHeight = 30`

# Architecture decisions now fixed

- The left tree stays as the physical navigator
- The right side will become a type-driven workspace
- User-facing level configuration and level renaming should be removed from the UI
- `LevelIndex` stays only as an internal technical coordinate
- `NodeType` must become the main driver of screen selection and behavior
- Excel `v3` keeps `Levels` as a legacy transition layer
- Preferred technical depth strategy:
  - hidden `MaxLevels`
  - default value `10`
  - if safe future increases prove too invasive, first typed release may temporarily behave like a fixed depth of `10`
- The first `Network` tab version is file-based, not interactive
- File-based `Network` must include:
  - large preview in the form
  - `Open original`

# New planning artifact

- Root roadmap file created: `Roadmap.md`
- That file is now the authoritative implementation sequence for the next major development wave
- Any new AI session should read:
  1. `AGENTS.md`
  2. `docs/codex-handoff.md`
  3. `Roadmap.md`

# Immediate next implementation target

Start `Phase 0` from `Roadmap.md`:

1. Remove user-facing level setup from WinForms UI
2. Stop presenting levels as a user concept
3. Keep technical depth control internally
4. Preserve JSON and Excel `v3` compatibility while doing so

# Files now especially relevant

- `Roadmap.md`
- `AGENTS.md`
- `docs/codex-handoff.md`
- `Forms/SetupForm.cs`
- `Forms/MainForm.Layout.cs`
- `Forms/MainForm.Events.cs`
- `UiServices/KnowledgeBaseWorkshopUiWorkflowService.cs`
- `Services/KnowledgeBaseConfigurationWorkflowService.cs`
- `Services/KnowledgeBaseFormStateService.cs`
- `Models/KbNode.cs`
- `Models/KbNodeDetails.cs`
- `Models/SavedData.cs`
- `Services/KnowledgeBaseTreeMutationWorkflowService.cs`
- `Services/KnowledgeBaseExcelWorkbookParser.cs`
- `Services/KnowledgeBaseXlsxWriter.cs`

# Validation performed in this session

- No build/test run was needed for the roadmap itself because this session only added planning documentation
- The latest code validation before this planning step remains:
  - `dotnet build`: passed
  - `dotnet test`: passed, `141/141`

# Known strategic risks

- The domain model still has no persistent `NodeId`, while future composition/docs/network features require stable cross-links
- The current right panel is still level-driven/flat and must be replaced with a screen host
- `LevelIndex` is still used in several behavior rules, especially for technical fields and depth checks
- Excel `v3` can remain a transition layer, but it is not sufficient as the long-term typed-data format
- Embedded preview for image-based network files is straightforward; embedded PDF preview is a separate dependency decision and should not be assumed for the first network phase

# Commands to run before finishing future implementation work

```powershell
dotnet build C:\Users\Olga\AKB5\asutpKB.csproj --configuration Release --no-restore
dotnet test C:\Users\Olga\AKB5\tests\AsutpKnowledgeBase.Core.Tests\AsutpKnowledgeBase.Core.Tests.csproj --configuration Release --no-build --no-restore
```
