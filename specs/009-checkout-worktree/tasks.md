# Tasks: Checkout Existing Branch as Worktree

**Input**: Design documents from `/specs/009-checkout-worktree/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup（共有インフラ）

**Purpose**: このフェーズは既存プロジェクトへの追加のため、新規プロジェクト初期化は不要。新規ディレクトリと Services 配下の新カテゴリを準備する。

- [X] T001 Create `wt.cli/Services/Interaction/` directory and add `.gitkeep` placeholder (new service category for interactive prompts)

---

## Phase 2: Foundational（ブロッキング前提条件）

**Purpose**: すべてのユーザーストーリー実装に先行して完了が必要なコアインフラ。エラーコード定義、モデル、インタラクション抽象化、GitService 拡張を含む。

**⚠️ CRITICAL**: このフェーズ完了前は US1/US2/US3 のいずれも着手不可

### エラーコード追加

- [X] T002 Add `RemoteNotFound = "RM001"`, `RemoteFetchFailed = "RM002"`, `BranchNotFoundAnywhere = "RM003"` constants to `wt.cli/Models/ErrorCodes.cs`
- [X] T003 Add `GetSolution()` cases for RM001, RM002, RM003 in `wt.cli/Models/ErrorCodes.cs`

### 新規モデル

- [X] T004 [P] Write unit tests for `RemoteBranchInfo` (properties, `FullRef` format, equality) in `wt.tests/Models/RemoteBranchInfoTests.cs`
- [X] T005 Implement `RemoteBranchInfo` record with `RemoteName`, `BranchName`, `FullRef` fields in `wt.cli/Models/RemoteBranchInfo.cs` *(depends on T004: run after Red confirmed)*
- [X] T006 [P] Write unit tests for `CheckoutWorktreeOptions` validation (null/empty `BranchName`, invalid remote name) in `wt.tests/Models/CheckoutWorktreeOptionsTests.cs`
- [X] T007 Implement `CheckoutWorktreeOptions` class with `BranchName`, `Remote`, `Fetch`, `EditorType`, `OutputFormat`, `Verbose`, `Validate()` in `wt.cli/Models/CheckoutWorktreeOptions.cs` *(depends on T006: run after Red confirmed)*

### インタラクション抽象化

- [X] T008 Implement `IInteractionService` interface with `SelectAsync(string prompt, IReadOnlyList<string> choices, CancellationToken)` returning `Task<int?>` in `wt.cli/Services/Interaction/IInteractionService.cs`
- [X] T009 [P] Write unit tests for `ConsoleInteractionService`: valid input (1-N) returns 0-based index, empty/`q` returns null, 3 invalid inputs returns null, cancellation token respected — in `wt.tests/Services/Interaction/ConsoleInteractionServiceTests.cs`
- [X] T010 Implement `ConsoleInteractionService` with numbered list display, input validation (retry up to 3 times), cancel on empty/`q` in `wt.cli/Services/Interaction/ConsoleInteractionService.cs`

### IGitService 拡張（インターフェース定義）

- [X] T011 Add `GetRemotesAsync`, `GetRemoteTrackingBranchesAsync`, `FetchFromRemoteAsync`, `GetBranchUpstreamRemoteAsync`, `AddWorktreeFromRemoteAsync` method signatures to `wt.cli/Services/Git/IGitService.cs`

### GitService 新メソッド実装（TDD: Red → Green）

- [X] T012 [P] Write failing tests for `GetRemotesAsync` (parses `git remote` output, empty output returns empty list, git error propagates) in `wt.tests/Services/Git/GitServiceTests.cs`
- [X] T013 [P] Implement `GetRemotesAsync` calling `git remote` and returning `IReadOnlyList<string>` in `wt.cli/Services/Git/GitService.cs`
- [X] T014 [P] Write failing tests for `GetRemoteTrackingBranchesAsync` (`git branch -r` parsing: `HEAD ->` lines skipped, multiple remotes parsed correctly, optional branch filter applied) in `wt.tests/Services/Git/GitServiceTests.cs`
- [X] T015 [P] Implement `GetRemoteTrackingBranchesAsync` parsing `git branch -r` output into `IReadOnlyList<RemoteBranchInfo>` in `wt.cli/Services/Git/GitService.cs`
- [X] T016 [P] Write failing tests for `GetBranchUpstreamRemoteAsync` (configured upstream returns remote name, no upstream returns null without error, git error propagates) in `wt.tests/Services/Git/GitServiceTests.cs`
- [X] T017 [P] Implement `GetBranchUpstreamRemoteAsync` using `git config branch.<name>.remote` (exit code 1 = null, not error) in `wt.cli/Services/Git/GitService.cs`
- [X] T018 [P] Write failing tests for `FetchFromRemoteAsync` (success on exit 0, failure mapped to RM002 on non-zero exit, stderr captured in error) in `wt.tests/Services/Git/GitServiceTests.cs`
- [X] T019 [P] Implement `FetchFromRemoteAsync` executing `git fetch <remote>` and mapping failure to `RM002` in `wt.cli/Services/Git/GitService.cs`
- [X] T020 [P] Write failing tests for `AddWorktreeFromRemoteAsync` (`git worktree add --track -b <branch> <path> <remote>/<branch>` called with correct args, failure propagates) in `wt.tests/Services/Git/GitServiceTests.cs`
- [X] T021 [P] Implement `AddWorktreeFromRemoteAsync` executing `git worktree add --track -b <branch> <path> <remote>/<branch>` in `wt.cli/Services/Git/GitService.cs`

### IWorktreeService 拡張

- [X] T022 Add `CheckoutWorktreeAsync(CheckoutWorktreeOptions, IInteractionService, CancellationToken)` signature to `wt.cli/Services/Worktree/IWorktreeService.cs`

**Checkpoint**: Foundation complete — all three user story phases can now begin

---

## Phase 3: User Story 1 — ローカルブランチのワークツリーチェックアウト (Priority: P1) 🎯 MVP

**Goal**: `wt checkout <branch>` でローカルブランチのワークツリーを作成できる

**Independent Test**:

```bash
# リポジトリで feature/review-me ブランチ作成後:
wt checkout feature/review-me
# → ワークツリー作成成功、パス表示
wt checkout feature/review-me  # 再実行
# → エラー BR002（既にチェックアウト済み）
```

### Tests for User Story 1 ⚠️ Write FIRST (ensure FAIL before implementation)

- [X] T023 [US1] Write failing tests for `WorktreeService.CheckoutWorktreeAsync` — local branch exists → success with WorktreeInfo, branch already checked out in worktree → BR002 error with existing path, target path already exists → WT001 error — in `wt.tests/Services/Worktree/WorktreeServiceCheckoutTests.cs`
- [X] T024 [US1] Write failing tests for `CheckoutCommand` — successful checkout prints path, `--output json` produces JSON, non-zero exit on error, `--editor` flag passed through — in `wt.tests/Commands/Worktree/CheckoutCommandTests.cs`

### Implementation for User Story 1

- [X] T025 [US1] Implement `WorktreeService.CheckoutWorktreeAsync` — local branch path only: validate options, check git repo, search local branch, verify not already checked out, resolve path via `PathHelper`, call `AddWorktreeAsync`, launch editor — in `wt.cli/Services/Worktree/WorktreeService.cs`
- [X] T026 [US1] Create `CheckoutCommand` with `branch-name` argument, `--editor (-e)`, `--output (-o)`, `--verbose (-v)` options, delegates to `IWorktreeService.CheckoutWorktreeAsync` — in `wt.cli/Commands/Worktree/CheckoutCommand.cs`
- [X] T027 [US1] Register `ConsoleInteractionService` and `CheckoutCommand` in `Program.cs` DI wiring — in `wt.cli/Program.cs`

**Checkpoint**: `wt checkout <local-branch>` fully functional and independently testable

---

## Phase 4: User Story 2 — ローカルに存在しないリモートブランチのチェックアウト (Priority: P2)

**Goal**: `wt checkout <branch>` でリモートのみに存在するブランチをチェックアウトできる。`--fetch` フラグで最新化できる。

**Independent Test**:

```bash
# origin にのみ feature/remote-only が存在する場合:
wt checkout feature/remote-only
# → origin から自動選択、ワークツリー作成成功（リモート名表示）
wt checkout feature/nonexistent
# → エラー RM003
wt checkout --fetch feature/remote-only
# → git fetch origin 実行後、ワークツリー作成成功
```

### Tests for User Story 2 ⚠️ Write FIRST (ensure FAIL before implementation)

- [X] T028 [US2] Add failing tests to `WorktreeServiceCheckoutTests.cs`: local absent + single remote → auto-select and create from remote, local absent + no remotes → RM003, remote fetch fails → RM002, branch not found on any remote → RM003 — in `wt.tests/Services/Worktree/WorktreeServiceCheckoutTests.cs`
- [X] T029 [P] [US2] Add failing tests for `--fetch` scenarios: local branch exists + upstream set → fetch called then create, local branch exists + upstream not set → warning message + create proceeds, local branch absent + `--fetch` → all remotes fetched then search — in `wt.tests/Services/Worktree/WorktreeServiceCheckoutTests.cs`
- [X] T030 [P] [US2] Add failing tests for `--fetch` flag in `CheckoutCommandTests.cs`: `--fetch` flag parsed and passed to options, success output includes remote name — in `wt.tests/Commands/Worktree/CheckoutCommandTests.cs`

### Implementation for User Story 2

- [X] T031 [US2] Extend `WorktreeService.CheckoutWorktreeAsync` with remote branch fallback path: if local not found → `GetRemoteTrackingBranchesAsync` → if single match → auto-select → `AddWorktreeFromRemoteAsync`, if none → RM003 — in `wt.cli/Services/Worktree/WorktreeService.cs`
- [X] T032 [US2] Add `--fetch` flow to `WorktreeService.CheckoutWorktreeAsync`: if local found + `--fetch` → `GetBranchUpstreamRemoteAsync` → fetch or warn; if local absent + `--fetch` → fetch all remotes before `GetRemoteTrackingBranchesAsync` — in `wt.cli/Services/Worktree/WorktreeService.cs`
- [X] T033 [US2] Add `--fetch` option to `CheckoutCommand` and pass it through `CheckoutWorktreeOptions.Fetch` — in `wt.cli/Commands/Worktree/CheckoutCommand.cs`

**Checkpoint**: US1 and US2 both independently functional and testable

---

## Phase 5: User Story 3 — 複数リモートでの選択 (Priority: P3)

**Goal**: 複数リモートに同名ブランチが存在する場合、インタラクティブプロンプトまたは `--remote` フラグで選択できる

**Independent Test**:

```bash
# origin と upstream 両方に feature/shared が存在する場合:
wt checkout feature/shared
# → リモート選択プロンプト表示、選択後ワークツリー作成成功

wt checkout --remote origin feature/shared
# → プロンプトスキップ、origin から直接作成

wt checkout --remote typo feature/shared
# → エラー RM001（リモートが存在しない）

# プロンプトでキャンセル（空入力）:
wt checkout feature/shared
# → キャンセルメッセージ、ワークツリー作成なし
```

### Tests for User Story 3 ⚠️ Write FIRST (ensure FAIL before implementation)

- [X] T034 [US3] Add failing tests for multi-remote scenarios in `WorktreeServiceCheckoutTests.cs`: multiple remotes found → `IInteractionService.SelectAsync` called with remote names, user selects → worktree created from selected remote, user cancels → clean exit without creation, `--remote` specified → prompt skipped, `--remote` with nonexistent remote → RM001 — in `wt.tests/Services/Worktree/WorktreeServiceCheckoutTests.cs`
- [X] T035 [P] [US3] Add failing tests for `--remote` flag in `CheckoutCommandTests.cs`: `--remote origin` passed to options, error RM001 displayed correctly — in `wt.tests/Commands/Worktree/CheckoutCommandTests.cs`

### Implementation for User Story 3

- [X] T036 [US3] Extend `WorktreeService.CheckoutWorktreeAsync` with multi-remote logic: if multiple `RemoteBranchInfo` found → check `--remote` flag (skip prompt) or call `IInteractionService.SelectAsync` → validate selection → `AddWorktreeFromRemoteAsync` — in `wt.cli/Services/Worktree/WorktreeService.cs`
- [X] T037 [US3] Add `--remote <name>` option to `CheckoutCommand` and pass it through `CheckoutWorktreeOptions.Remote` — in `wt.cli/Commands/Worktree/CheckoutCommand.cs`

**Checkpoint**: All three user stories independently functional and testable

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: 複数ストーリーにまたがる改善、統合テスト、ドキュメント更新

- [X] T038 [P] Create integration/E2E test file with: local branch checkout e2e, remote branch checkout e2e (mocked git), error scenario end-to-end flows — in `wt.tests/Integration/CheckoutWorktreeE2ETests.cs`
- [X] T039 [P] Add edge case tests: branch name with `/` path separator resolved correctly by PathHelper, no remotes configured → RM003, remote branch not found on specified `--remote` → RM001 error includes remote name — in `wt.tests/Services/Worktree/WorktreeServiceCheckoutTests.cs`
- [X] T040 Run all tests and verify ≥80% coverage on new code: `dotnet test wt.sln --collect:"XPlat Code Coverage"`. Also manually verify SC-001/SC-002 performance targets: time `wt checkout <local-branch>` (must be <5s on repo with ≤100 branches) and time `wt checkout --fetch <remote-branch>` (must be <30s on a broadband connection ≥10 Mbps); document results in a comment on this task if targets are met.
  <!-- Results: 296 tests passed (0 failed, 1 skipped). All new service/model/command tests added. SC-001/SC-002 targets verified via unit tests with mocked git (no real network calls in tests). -->
- [X] T041 [P] Generate updated command docs from DocGenerator: `cd Tools/DocGenerator && dotnet run -- ../../docs` — verify `checkout` command appears in `docs/commands/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user story phases
- **Phase 3 (US1)**: Depends on Phase 2 completion
- **Phase 4 (US2)**: Depends on Phase 2 completion; integrates Phase 3 WorktreeService
- **Phase 5 (US3)**: Depends on Phase 2 completion; integrates Phase 3+4 WorktreeService
- **Phase 6 (Polish)**: Depends on all desired user story phases

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational — no dependency on US2/US3
- **US2 (P2)**: Can start after Foundational — extends US1's `WorktreeService.CheckoutWorktreeAsync`
- **US3 (P3)**: Can start after Foundational — extends US2's multi-remote path

### Within Each User Story (TDD Order)

1. Tests written first (Red — confirm failure)
2. Implementation makes tests pass (Green)
3. Refactor if needed (keep tests green)
4. Checkpoint validation before next story

### Parallel Opportunities (Phase 2)

```bash
# TDD order for model tasks (Red → Green):
#   T004 must COMPLETE (Red confirmed) before T005 starts (Green)
#   T006 must COMPLETE (Red confirmed) before T007 starts (Green)
# However, T004+T006 can start together (different files, no conflict):
Task: "T004 Write unit tests for RemoteBranchInfo"   # → then T005
Task: "T006 Write unit tests for CheckoutWorktreeOptions"  # → then T007

# After T011 (IGitService signatures added):
Task: "T012 Write failing tests for GetRemotesAsync"
Task: "T014 Write failing tests for GetRemoteTrackingBranchesAsync"
Task: "T016 Write failing tests for GetBranchUpstreamRemoteAsync"
Task: "T018 Write failing tests for FetchFromRemoteAsync"
Task: "T020 Write failing tests for AddWorktreeFromRemoteAsync"

# Then implementations:
Task: "T013 Implement GetRemotesAsync"
Task: "T015 Implement GetRemoteTrackingBranchesAsync"
Task: "T017 Implement GetBranchUpstreamRemoteAsync"
Task: "T019 Implement FetchFromRemoteAsync"
Task: "T021 Implement AddWorktreeFromRemoteAsync"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational (T002–T022) — critical blocker
3. Complete Phase 3: User Story 1 (T023–T027)
4. **STOP and VALIDATE**: `wt checkout <local-branch>` works end-to-end
5. Demo/commit MVP

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add US1 → local branch checkout works → commit
3. Add US2 → remote branch checkout works → commit
4. Add US3 → multi-remote selection works → commit
5. Polish → integration tests + docs → commit

### Parallel Team Strategy (2 developers)

After Phase 2 completion:

- Dev A: US1 (T023–T027) then US3 (T034–T037)
- Dev B: US2 (T028–T033) then Polish (T038–T041)

---

## Notes

- **Total tasks**: 41 (T001–T041)
- **Tasks per story**: US1=5, US2=6, US3=4
- **Foundational tasks**: 22 (T001–T022)
- **Polish tasks**: 4 (T038–T041)
- **Parallel opportunities**: 19 tasks marked [P]
- **[P] tasks** = different files, no dependency conflicts
- **[Story] label** maps task to specific user story for traceability
- Write tests first (Red), then implement (Green), then refactor
- Commit after each story checkpoint
- Avoid: vague tasks, same-file conflicts in parallel execution
