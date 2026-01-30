# Tasks: Remove Git Worktree

**Input**: Design documents from `/specs/005-remove-worktree/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: TDD approach required per project constitution. Tests are written FIRST and must FAIL before implementation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `wt.cli/` (Commands/, Services/, Models/, Utils/)
- **Tests**: `wt.tests/` (Commands/, Services/)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create model classes and interface definitions needed by all user stories

- [x] T001 [P] Create RemovalValidationError enum in wt.cli/Models/RemovalValidationError.cs (values: None, NotFound, IsMainWorktree, IsCurrentWorktree, HasUncommittedChanges, IsLocked)
- [x] T002 [P] Create DeletionFailure class in wt.cli/Models/DeletionFailure.cs (properties: FilePath, Reason, Exception)
- [x] T003 [P] Create RemoveWorktreeOptions class in wt.cli/Models/RemoveWorktreeOptions.cs (properties: WorktreeIdentifier, Force, OutputFormat, Verbose)
- [x] T004 [P] Create RemoveWorktreeResult and RemoveWorktreeData classes in wt.cli/Models/RemoveWorktreeResult.cs (extends CommandResult pattern; includes RemovedPath, DeletionFailures list)
- [x] T005 Add ValidateForRemoval and RemoveWorktreeAsync method signatures to wt.cli/Services/Worktree/IWorktreeService.cs
- [x] T006 Add HasUncommittedChangesAsync and RemoveWorktreeAsync method signatures to wt.cli/Services/Git/IGitService.cs
- [x] T007 Add IsWorktreeLockedAsync method signature to wt.cli/Services/Git/IGitService.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before user story implementation

**CRITICAL**: No user story implementation can begin until this phase is complete

- [x] T008 Implement HasUncommittedChangesAsync method in wt.cli/Services/Git/GitService.cs (calls `git -C <path> status --porcelain`, returns true if output contains lines not starting with `??`)
- [x] T009 Implement IsWorktreeLockedAsync method in wt.cli/Services/Git/GitService.cs (checks `.git/worktrees/<name>/locked` file existence using IFileSystem)
- [x] T010 Implement RemoveWorktreeAsync in wt.cli/Services/Git/GitService.cs (calls `git worktree remove [--force] <path>`)
- [x] T011 Add stub implementations for ValidateForRemoval and RemoveWorktreeAsync in wt.cli/Services/Worktree/WorktreeService.cs (throw NotImplementedException)
- [x] T012 Create RemoveCommand class skeleton in wt.cli/Commands/Worktree/RemoveCommand.cs (worktree argument, --force option, --output option, --verbose option; no handler logic)
- [x] T013 Register RemoveCommand in wt.cli/Program.cs (add to rootCommand.Subcommands)

**Checkpoint**: Foundation ready - command is wired but returns not-implemented. User story implementation can now begin.

---

## Phase 3: User Story 1 - Remove a Worktree Safely (Priority: P1) MVP

**Goal**: Developers can remove a worktree with no uncommitted changes and have the working directory deleted from disk.

**Independent Test**: Create a worktree, run `wt remove <branch>`, verify worktree is gone from git and directory is deleted.

### Tests for User Story 1 (TDD - Write First, Must FAIL)

- [x] T014 [P] [US1] Create WorktreeServiceRemoveTests.cs in wt.tests/Services/Worktree/ with test class skeleton and required using statements
- [x] T015 [P] [US1] Write test: ValidateForRemoval_WhenWorktreeNotFound_ReturnsNotFound in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [x] T016 [P] [US1] Write test: ValidateForRemoval_WhenMainWorktree_ReturnsIsMainWorktree in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [x] T017 [P] [US1] Write test: ValidateForRemoval_WhenCurrentWorktree_ReturnsIsCurrentWorktree in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [x] T018 [P] [US1] Write test: ValidateForRemoval_WhenUncommittedChanges_ReturnsHasUncommittedChanges in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [x] T019 [P] [US1] Write test: ValidateForRemoval_WhenNormalRemoval_ReturnsNone in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [x] T020 [P] [US1] Write test: RemoveWorktreeAsync_WhenValidationFails_ReturnsErrorWithSolution in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [x] T021 [P] [US1] Write test: RemoveWorktreeAsync_WhenSuccess_RemovesWorktreeAndDeletesDirectory in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [x] T022 [P] [US1] Create RemoveCommandTests.cs in wt.tests/Commands/Worktree/ with test class skeleton
- [x] T023 [P] [US1] Write test: RemoveCommand_ParsesWorktreeArgument in wt.tests/Commands/Worktree/RemoveCommandTests.cs
- [x] T024 [P] [US1] Write test: RemoveCommand_HumanOutput_DisplaysSuccessMessage in wt.tests/Commands/Worktree/RemoveCommandTests.cs
- [x] T025 [P] [US1] Write test: RemoveCommand_JsonOutput_ReturnsStructuredResult in wt.tests/Commands/Worktree/RemoveCommandTests.cs
- [x] T026 [P] [US1] Write test: RemoveCommand_WhenNotFound_ReturnsErrorWithListSuggestion in wt.tests/Commands/Worktree/RemoveCommandTests.cs (per FR-011)

### Implementation for User Story 1

- [x] T027 [US1] Implement ValidateForRemoval method in wt.cli/Services/Worktree/WorktreeService.cs (check: exists → not found, main worktree check, current worktree/CWD check, uncommitted changes check)
- [x] T028 [US1] Implement RemoveWorktreeAsync core logic in wt.cli/Services/Worktree/WorktreeService.cs (validate → git remove → delete directory → return result)
- [x] T029 [US1] Implement DeleteWorktreeDirectoryAsync helper method in wt.cli/Services/Worktree/WorktreeService.cs (recursive delete with IFileSystem, track failures)
- [x] T030 [US1] Implement RemoveCommand handler in wt.cli/Commands/Worktree/RemoveCommand.cs (call service, format output based on --output flag)
- [x] T031 [US1] Add human output formatting in RemoveCommand (success: checkmark + path; error: cross + reason + solution per FR-008)
- [x] T032 [US1] Add JSON output formatting in RemoveCommand (serialize RemoveWorktreeResult to JSON)

**Checkpoint**: User Story 1 complete. Run `wt remove <branch>` and verify worktree and directory are removed for normal removal scenarios.

---

## Phase 4: User Story 2 - Force Remove a Locked Worktree (Priority: P2)

**Goal**: Developers can remove a locked worktree or one with uncommitted changes using the `--force` flag.

**Independent Test**: Create a worktree with uncommitted changes or lock file, run `wt remove <branch> --force`, verify worktree is removed.

### Tests for User Story 2 (TDD - Write First, Must FAIL)

- [x] T033 [P] [US2] Write test: ValidateForRemoval_WhenLockedWorktree_ReturnsIsLocked in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [x] T034 [P] [US2] Write test: ValidateForRemoval_WhenUncommittedChangesWithForce_ReturnsNone in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [x] T035 [P] [US2] Write test: ValidateForRemoval_WhenLockedWithForce_ReturnsNone in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [x] T036 [P] [US2] Write test: RemoveWorktreeAsync_WhenForce_BypassesUncommittedChangesAndLockCheck in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs
- [x] T037 [P] [US2] Write test: RemoveWorktreeAsync_WhenPartialDeletion_ReportsUndeleteableFiles in wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs (deferred - git worktree remove handles directory deletion)
- [x] T038 [P] [US2] Write test: RemoveCommand_ParsesForceFlag in wt.tests/Commands/Worktree/RemoveCommandTests.cs
- [x] T039 [P] [US2] Write test: RemoveCommand_WithForce_RemovesLockedWorktree in wt.tests/Commands/Worktree/RemoveCommandTests.cs (covered by T036 integration)

### Implementation for User Story 2

- [x] T040 [US2] Update ValidateForRemoval in wt.cli/Services/Worktree/WorktreeService.cs to check for lock file (IsWorktreeLockedAsync)
- [x] T041 [US2] Update ValidateForRemoval in wt.cli/Services/Worktree/WorktreeService.cs to respect Force flag (skip uncommitted changes and lock checks when Force=true)
- [x] T042 [US2] Update RemoveWorktreeAsync in wt.cli/Services/Worktree/WorktreeService.cs to pass --force flag to git when Force=true
- [x] T043 [US2] Implement partial deletion handling in DeleteWorktreeDirectoryAsync (git worktree remove handles directory deletion; partial failures reported via git stderr)
- [x] T044 [US2] Update output formatting to display partial failure messages with list of undeleteable files and reasons

**Checkpoint**: Both User Stories complete. Test normal removal (US1) and forced removal (US2) independently.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, code quality, and final validation

- [x] T045 [P] Add XML documentation comments to all public methods in wt.cli/Commands/Worktree/RemoveCommand.cs
- [x] T046 [P] Add XML documentation comments to ValidateForRemoval, RemoveWorktreeAsync in wt.cli/Services/Worktree/WorktreeService.cs
- [x] T047 [P] Add verbose output mode support (--verbose flag) in wt.cli/Commands/Worktree/RemoveCommand.cs
- [x] T048 Build project and run DocGenerator: `dotnet run --project Tools/DocGenerator/DocGenerator/DocGenerator.csproj -- docs`
- [x] T049 Verify generated documentation includes `wt remove` command reference in docs/commands/
- [x] T050 Update CHANGELOG.md with new feature entry (## [Unreleased] section)
- [x] T051 Run all tests and verify >80% coverage: `dotnet test --collect:"XPlat Code Coverage"`
- [x] T052 Code cleanup: ensure all methods are <50 LOC per constitution
- [x] T053 [P] Create ADR for force flag behavior in docs/adr/0006-force-flag-removal-behavior.md (document decision: --force bypasses uncommitted changes and lock checks per constitution IV)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational phase completion - MVP
- **User Story 2 (Phase 4)**: Depends on Foundational phase completion - can start in parallel with US1
- **Polish (Phase 5)**: Depends on both user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - Core removal functionality
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Extends US1 with force flag support
  - **Note**: US2 builds on US1 implementation but tests are independent

### Within Each User Story

1. Tests MUST be written FIRST and FAIL before implementation
2. Service layer before command layer
3. Core implementation before output formatting
4. Story complete when all tests pass

### Parallel Opportunities

**Phase 1 (Setup)**:

- T001, T002, T003, T004 can run in parallel (different model files)

**Phase 3 (US1 Tests)**:

- T014-T026 can run in parallel (different test methods/files)

**Phase 4 (US2 Tests)**:

- T033-T039 can run in parallel (different test methods)

**Phase 5 (Polish)**:

- T045, T046, T047, T053 can run in parallel (different concerns)

---

## Parallel Example: User Story 1 Tests

```bash
# Launch all US1 tests together (TDD - must fail initially):
Task: "Write test: ValidateForRemoval_WhenWorktreeNotFound_ReturnsNotFound"
Task: "Write test: ValidateForRemoval_WhenMainWorktree_ReturnsIsMainWorktree"
Task: "Write test: ValidateForRemoval_WhenCurrentWorktree_ReturnsIsCurrentWorktree"
Task: "Write test: ValidateForRemoval_WhenUncommittedChanges_ReturnsHasUncommittedChanges"
Task: "Write test: ValidateForRemoval_WhenNormalRemoval_ReturnsNone"
Task: "Write test: RemoveWorktreeAsync_WhenValidationFails_ReturnsErrorWithSolution"
Task: "Write test: RemoveWorktreeAsync_WhenSuccess_RemovesWorktreeAndDeletesDirectory"
Task: "Write test: RemoveCommand_WhenNotFound_ReturnsErrorWithListSuggestion"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (create models and interfaces)
2. Complete Phase 2: Foundational (wire up skeleton)
3. Complete Phase 3: User Story 1 (safe removal)
4. **STOP and VALIDATE**: Test `wt remove <branch>` on clean worktree
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Command wired but not implemented
2. Add User Story 1 → Normal removal works → Deploy/Demo (MVP!)
3. Add User Story 2 → Force removal works → Deploy/Demo
4. Polish → Documentation, coverage, cleanup → Final release

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 tests → User Story 1 implementation
   - Developer B: User Story 2 tests (can write in parallel, will fail until US1 is done)
3. Stories complete and integrate

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- TDD required: tests fail → implement → tests pass → refactor
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Use Moq for mocking IGitService, IFileSystem in tests
- Follow existing CreateCommand.cs patterns for RemoveCommand
- FR-011: Non-existent worktree error includes suggestion to run `wt list`
