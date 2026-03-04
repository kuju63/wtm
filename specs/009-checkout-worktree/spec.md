# Feature Specification: Checkout Existing Branch as Worktree

**Feature Branch**: `009-checkout-worktree`
**Created**: 2026-03-04
**Status**: Draft
**Input**: User description: "既存ブランチをcheckoutしてworktreeとして管理を行う。基本機能はcreateコマンドと同様にする。ユースケースとしてはコードレビューのためにremoteブランチもしくはローカルブランチを独立した環境で動作確認できること。原則としてローカルブランチを優先するが、指定のブランチが存在しない場合にはremoteリポジトリからチェックアウトを行う。複数のremoteリポジトリが存在する場合にはユーザーに対象のリポジトリを選択してもらう。remoteリポジトリはgit remoteコマンドで取得できるリポジトリとする。"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Check Out Local Branch as Worktree (Priority: P1)

A developer wants to review a colleague's pull request. They specify the branch name to the `checkout` command, and the tool creates an isolated worktree pointing to that local branch. The developer can then open the code in their editor without disturbing their current working state.

**Why this priority**: This is the most common use case—local branches are the default target and represent the minimum viable functionality for code review workflows.

**Independent Test**: Can be fully tested by running `wt checkout <branch-name>` on a repository with existing local branches and verifying that an isolated worktree is created, checkable independently of any remote.

**Acceptance Scenarios**:

1. **Given** a Git repository with a local branch `feature/review-me`, **When** the user runs `wt checkout feature/review-me`, **Then** a worktree is created at the standard path with that branch checked out, and the user is informed of the created worktree path.
2. **Given** a Git repository with a local branch, **When** the worktree is created successfully, **Then** the optional editor (if configured) opens at the worktree directory, matching the behavior of the `create` command.
3. **Given** the branch name is already checked out in another worktree, **When** the user runs `wt checkout <branch-name>`, **Then** the tool reports an error explaining the branch is already in use and suggests the existing worktree path.

---

### User Story 2 - Check Out Remote Branch as Worktree When Local Is Absent (Priority: P2)

A developer wants to review a remote branch that does not exist locally. They run `wt checkout <branch-name>`, the tool finds the branch only on the remote, fetches it, and creates a worktree for local review.

**Why this priority**: Enables the full code review workflow for branches that haven't been fetched locally yet, completing the primary use case of the feature.

**Independent Test**: Can be tested by checking out a branch that exists only on the remote (not locally) and verifying a worktree is created with the correct content.

**Acceptance Scenarios**:

1. **Given** a repository where `feature/remote-only` exists on a single remote but not locally, **When** the user runs `wt checkout feature/remote-only`, **Then** the tool fetches the branch from the remote and creates a worktree tracking that remote branch.
2. **Given** the remote branch fetch succeeds, **When** the worktree is created, **Then** the worktree is placed at the standard path and the user receives a confirmation message including the worktree path and the remote it was fetched from.
3. **Given** the remote branch does not exist on any configured remote, **When** the user runs `wt checkout <branch-name>`, **Then** the tool reports a clear error stating the branch was not found locally or on any remote.

---

### User Story 3 - Select Remote When Multiple Remotes Have the Branch (Priority: P3)

A developer's repository has multiple remotes (e.g., `origin` and `upstream`). Both have a branch with the same name. The tool prompts the user to choose which remote to use before creating the worktree. Alternatively, the developer can specify the remote upfront with `--remote <name>` to skip the prompt.

**Why this priority**: Necessary for correctness in multi-remote workflows; without this, the tool would silently pick the wrong source, leading to review of unintended code.

**Independent Test**: Can be tested by setting up a repository with two remotes both containing a branch of the same name, then running `wt checkout <branch-name>` and verifying the user is prompted to select a remote; and separately running `wt checkout --remote origin <branch-name>` to verify the prompt is skipped.

**Acceptance Scenarios**:

1. **Given** a repository with two remotes (`origin`, `upstream`) both containing branch `feature/shared`, **When** the user runs `wt checkout feature/shared`, **Then** the tool lists the available remotes and prompts the user to select one interactively.
2. **Given** the user selects a remote from the prompt, **Then** the branch is fetched from the selected remote and a worktree is created.
3. **Given** the user cancels the remote selection prompt, **Then** no worktree is created and the operation exits cleanly with a user-friendly message.
4. **Given** a repository with two remotes both containing `feature/shared`, **When** the user runs `wt checkout --remote origin feature/shared`, **Then** the interactive prompt is skipped and the worktree is created from `origin` directly.

---

### Edge Cases

- What happens when the specified branch name is ambiguous (matches both a local branch and a differently-named remote-tracking branch)? → Local branch takes priority per the stated rule.
- What happens when a worktree for the same branch already exists? → Report an error with the existing worktree location.
- What happens when no remotes are configured and the branch does not exist locally? → Report an error that the branch cannot be found.
- What happens when the remote fetch fails (network error, authentication)? → Report an actionable error describing the failure and suggest manual fetch steps.
- What happens when the target worktree directory already exists on the filesystem? → Report an error with a clear message (same behavior as `create` command).
- What happens when only one remote contains the branch? → Use that remote automatically without prompting.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The tool MUST provide a `checkout` command that accepts a branch name as a required argument.
- **FR-002**: The tool MUST first search local branches for the specified name; if found locally, it MUST use the local branch to create the worktree without accessing any remote.
- **FR-003**: If the branch is not found locally, the tool MUST search all remotes (discovered via `git remote`) using cached remote-tracking references by default.
- **FR-004**: If the branch is found on exactly one remote, the tool MUST automatically create a worktree tracking that remote branch without prompting the user.
- **FR-004a**: The `checkout` command MUST support a `--fetch` flag; when provided, the tool MUST run `git fetch` against the relevant remote(s) before searching for the branch.
- **FR-005**: If the branch is found on multiple remotes and no `--remote` flag is provided, the tool MUST present an interactive selection prompt listing the available remote names and wait for the user to choose one before proceeding.
- **FR-005a**: The `checkout` command MUST support a `--remote <name>` flag; when provided, the tool MUST skip the interactive prompt and use the specified remote directly, reporting an error if the branch does not exist on that remote.
- **FR-006**: If the branch does not exist locally or on any remote, the tool MUST report a descriptive error and exit without creating any worktree.
- **FR-007**: If the selected branch is already checked out in an existing worktree, the tool MUST report an error including the path of the existing worktree.
- **FR-008**: The worktree MUST be created at the same default path location as the `create` command (derived from the repository root and branch name via `PathHelper`).
- **FR-009**: The `checkout` command MUST support the same editor-launch option as the `create` command, opening the configured editor in the new worktree directory upon success.
- **FR-010**: All success and error messages MUST be consistent with the messaging style of the existing `create` and `list` commands.

### Key Entities

- **Branch**: A named reference to a sequence of commits, identified by its name; may exist locally, on one or more remotes, or both.
- **Remote**: A named external repository configured in the local Git repository, discoverable via `git remote`.
- **Worktree**: An isolated working directory linked to the repository, checked out to a specific branch; created and managed by Git.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can check out a local branch as a worktree with a single command in under 5 seconds on a repository with up to 100 local branches.
- **SC-002**: Users can check out a remote-only branch as a worktree with a single command, including the fetch step, in under 30 seconds on a broadband connection of at least 10 Mbps with a remote repository of typical size (≤500 MB).
- **SC-003**: When multiple remotes contain the target branch, users are presented with a clear selection prompt and can complete the worktree creation with one additional interaction.
- **SC-004**: 100% of error scenarios (branch not found, already checked out, directory conflict) produce a human-readable message with sufficient context for the user to resolve the issue without consulting documentation.
- **SC-005**: The `checkout` command shares the same option names and behaviors as `create` for all overlapping features: `--editor (-e)`, `--output (-o json|table)`, `--verbose (-v)`; success and error message formats follow the same structure as `create`.

## Assumptions

- The worktree placement path and directory naming (including transformation of branch names containing path separators such as `/`) follow the same convention as the `create` command via `PathHelper`; no new naming strategy is introduced.
- `git remote` is the authoritative source of configured remotes; no other remote discovery mechanism is needed.
- Interactive prompts are presented on standard output/input (TTY); non-interactive environments are out of scope for this feature.
- The editor integration is optional and governed by the same configuration mechanism as the `create` command.
- By default, remote branch lookup uses cached remote-tracking references; explicit remote fetch is performed only when the `--fetch` flag is provided.

## Clarifications

### Session 2026-03-04

- Q: Remote branch lookup strategy before searching → A: Default uses cached remote-tracking references; `git fetch` is only performed when the `--fetch` flag is explicitly provided by the user
- Q: Explicit remote specification flag → A: Support `--remote <name>` flag to bypass interactive prompt and target a specific remote directly
- Q: Worktree directory naming for branch names containing path separators (e.g., `feature/review-me`) → A: Apply the same transformation rules as the `create` command via `PathHelper`; no new naming strategy is introduced
