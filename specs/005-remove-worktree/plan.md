# Implementation Plan: Remove Git Worktree

**Branch**: `005-remove-worktree` | **Date**: 2026-01-24 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/005-remove-worktree/spec.md`

**Note**: This plan is produced by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement a new CLI command to safely remove git worktrees and their associated working directories from disk. The feature prioritizes developer safety through explicit targeting (no confirmation prompts), protection of the main worktree and currently-checked-out worktrees, and graceful handling of locked/problematic worktrees via an optional `--force` flag. Uncommitted changes will block removal unless force is used, aligning with git's safety model.

## Technical Context

**Language/Version**: C# / .NET 10.0
**Primary Dependencies**: System.CommandLine 2.0.2 (CLI parsing), System.IO.Abstractions 22.1.0 (file system)
**Storage**: File system operations (worktree directory management); git metadata management via git command-line
**Testing**: xUnit 2.9.3 with Shouldly 4.3.0 (fluent assertions) and Moq 4.20.72 (mocking)
**Target Platform**: Cross-platform (Windows x64, Linux x64/ARM, macOS ARM64); self-contained single-file executables
**Project Type**: CLI single-project application with service abstraction layer
**Performance Goals**: Instantaneous removal (sub-second execution for typical worktrees, <100ms for metadata cleanup)
**Constraints**: No external service dependencies; must respect OS-level permission restrictions; graceful handling of partial failures
**Scale/Scope**: Single command addition to existing `wt` CLI; aligns with existing `create` and `list` command patterns

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Justification |
|-----------|--------|---------------|
| **I. Developer Usability (CLI Priority)** | ✅ PASS | Feature adds new `remove` command following verb-noun pattern (existing model: `create`, `list`). Help messages and JSON output format integrated. |
| **II. Cross-Platform** | ✅ PASS | Uses System.IO.Abstractions for OS-agnostic file operations; no OS-specific APIs. Existing build matrix covers Windows/Linux/macOS. |
| **III. Clean & Secure Code** | ✅ PASS | Extends existing service architecture (IWorktreeService). Input validation via existing validators. No hardcoded paths or secrets. |
| **IV. Documentation Clarity** | ✅ PASS | Technical decisions recorded in clarifications section of spec. ADR will be created for force/uncommitted-changes behavior. |
| **V. Minimal Dependencies** | ✅ PASS | Zero new dependencies required; uses existing System.CommandLine and System.IO.Abstractions. |
| **VI. Comprehensive Testing** | ✅ PASS | TDD approach enforced: tests written before implementation. Integration + unit tests planned (integration: e2e removal; unit: worktree validation). |
| **VII. Quantitative Thresholds** | ✅ PASS | Method LOC target <50 (removal logic is straightforward); cyclomatic complexity <8 (simple if/else guard clauses); test coverage >80% required. |

**Gate Status**: ✅ **PASS** — Feature aligns with all constitutional principles.

## Project Structure

### Documentation (this feature)

```text
specs/005-remove-worktree/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
wt.cli/
├── Commands/
│   ├── Worktree/
│   │   ├── CreateCommand.cs
│   │   └── RemoveCommand.cs           [NEW - this feature]
│   └── ListCommand.cs
├── Services/
│   ├── Worktree/
│   │   ├── IWorktreeService.cs        [extend: add RemoveWorktree methods]
│   │   └── WorktreeService.cs         [extend: implement RemoveWorktree + validation]
│   ├── Git/
│   │   ├── IGitService.cs             [extend: add HasUncommittedChanges, RemoveWorktree methods]
│   │   └── GitService.cs              [extend: implement HasUncommittedChanges, RemoveWorktree]
│   ├── Editor/
│   │   └── IEditorService.cs
│   └── Output/
│       └── OutputService.cs
├── Models/
│   ├── RemoveWorktreeOptions.cs       [NEW]
│   ├── RemoveWorktreeResult.cs        [NEW]
│   ├── RemovalValidationError.cs      [NEW enum]
│   └── DeletionFailure.cs             [NEW]
├── Formatters/
│   └── TableFormatter.cs              [reuse: output formatting]
├── Utils/
│   ├── IProcessRunner.cs              [reuse: git execution]
│   ├── PathHelper.cs                  [reuse: path handling]
│   └── Validators.cs                  [extend: add validation helpers]
└── Program.cs                         [extend: register RemoveCommand]

wt.tests/
├── Commands/
│   ├── Worktree/
│   │   ├── CreateCommandTests.cs
│   │   └── RemoveCommandTests.cs      [NEW]
│   └── ListCommandTests.cs
└── Services/
    ├── Worktree/
    │   └── WorktreeServiceRemoveTests.cs   [NEW]
    └── Git/
        └── GitServiceTests.cs
```

**Structure Decision**: Extends existing .NET CLI architecture with minimal new code:

- 1 new command class (RemoveCommand.cs) following System.CommandLine pattern
- 4 new model classes (RemoveWorktreeOptions, RemoveWorktreeResult, RemovalValidationError, DeletionFailure)
- 2 new methods in existing IWorktreeService and IGitService interfaces
- 2 new test files (unit tests only; E2E deferred to future infrastructure)
- Reuses existing error handling, output formatting, and dependency injection patterns
- All changes follow existing verb-noun command model and service architecture
- File system operations use existing IFileSystem abstraction (System.IO.Abstractions)

## Key Definitions (from Spec)

Per the spec Definitions section, the following measurable criteria are used:

- **Locked Worktree**: Detected by presence of `.git/worktrees/<name>/locked` file
- **Uncommitted Changes**: Staged changes or unstaged modifications (untracked files excluded); detected via `git status --porcelain`
- **--force Required When**: Either lock file present OR uncommitted changes exist
- **Main Worktree**: `.git` in worktree path is a directory (not a file)
- **Current Worktree**: CWD is within the worktree's path

## Complexity Tracking

**No violations** — All changes comply with constitution thresholds. No complexity tracking required.
