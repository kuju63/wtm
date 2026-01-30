# Pre-Implementation Review Checklist: Remove Git Worktree

**Purpose**: Validate requirements quality, completeness, and clarity before implementation begins
**Created**: 2026-01-24
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md) | [tasks.md](../tasks.md)
**Focus**: Full Scope (CLI design, safety validations, error handling, edge cases)

**Note**: This checklist tests the REQUIREMENTS themselves (clarity, completeness, consistency) - not implementation correctness.

---

## Requirement Completeness

- [x] CHK001 - Are all worktree identifier input formats explicitly defined (name vs path vs branch)? [Completeness, Spec §FR-001]
- [x] CHK002 - Is the working directory deletion behavior specified for all file states (clean, modified, locked)? [Completeness, Spec §FR-003]
- [x] CHK003 - Are the criteria for "main worktree" detection explicitly defined? [Completeness, Spec §FR-005]
- [x] CHK004 - Is "currently checked out in the active session" precisely defined (same terminal, same process, any wt session)? [Completeness, Spec §FR-006]
- [x] CHK005 - Are "uncommitted changes" explicitly defined (staged, unstaged, untracked files)? [Completeness, Spec §FR-007]
- [x] CHK006 - Is the order of operations specified (validate → remove from git → delete directory)? [Completeness, Gap]
- [x] CHK007 - Are requirements defined for handling worktrees on network/remote paths? [Completeness, Edge Case] → FR-016

---

## Requirement Clarity

- [x] CHK008 - Is "locked worktree" quantified with specific conditions that constitute a lock? [Clarity, Spec §FR-004]
- [x] CHK009 - Is "problematic worktrees" defined with specific error conditions? [Clarity, Spec §FR-004]
- [x] CHK010 - Is "clear error messages" specified with required content (error code, user action)? [Clarity, Spec §FR-008]
- [x] CHK011 - Is the definition of "safely" in User Story 1 measurable? [Clarity, Spec US1]
- [x] CHK012 - Are "clean up" operations in User Story 1 explicitly enumerated? [Clarity, Spec US1]
- [x] CHK013 - Is "forcibly removed" distinguished from normal removal with specific behaviors? [Clarity, Spec US2]

---

## Requirement Consistency

- [x] CHK014 - Are force flag behaviors consistent between US2 acceptance scenarios and FR-004/FR-007? [Consistency]
- [x] CHK015 - Is the error handling behavior consistent between FR-008 and FR-010 (report failures)? [Consistency]
- [x] CHK016 - Are directory deletion requirements in FR-003 consistent with partial deletion in FR-010? [Consistency]
- [x] CHK017 - Are the assumptions about "standard git worktree conventions" aligned with FR definitions? [Consistency, Assumptions]

---

## Acceptance Criteria Quality

- [x] CHK018 - Can "worktree is removed from the git repository" in US1-AS1 be objectively verified? [Measurability]
- [x] CHK019 - Can "working directory is deleted from disk" be verified with specific assertions? [Measurability]
- [x] CHK020 - Is SC-002 ("100% of normal worktree removal operations") achievable and measurable? [Measurability, SC-002]
- [x] CHK021 - Can SC-004 ("clearly communicate why a removal failed") be objectively evaluated? [Measurability, SC-004]
- [x] CHK022 - Are the conditions for "first attempt" success in SC-002 defined? [Measurability, SC-002]

---

## User Story Coverage

- [x] CHK023 - Are requirements defined for both single worktree and multi-worktree repository scenarios? [Coverage]
- [x] CHK024 - Are requirements specified for removing a worktree when other worktrees have uncommitted changes? [Coverage, Interaction]
- [x] CHK025 - Is the behavior specified when the target worktree path no longer exists on disk? [Coverage, Edge Case §1] → FR-012
- [x] CHK026 - Is the behavior specified when git worktree entry exists but directory is already deleted? [Coverage, Edge Case §2] → FR-012
- [x] CHK027 - Are recovery flows defined if removal fails midway (git removed but directory not deleted)? [Coverage, Recovery Flow] → FR-017

---

## Edge Case Coverage

- [x] CHK028 - Is behavior defined when developer attempts to remove a non-existent worktree? [Edge Case, Spec Edge Case §1]
- [x] CHK029 - Is behavior defined for removing the worktree the user is currently inside of? [Edge Case, Spec Edge Case §3]
- [x] CHK030 - Are permission error scenarios explicitly documented with expected behavior? [Edge Case, Spec Edge Case §4]
- [x] CHK031 - Is partial deletion (some files locked) behavior fully specified with all outcomes? [Edge Case, Spec Edge Case §5]
- [x] CHK032 - Is behavior defined for worktrees with special characters in path or branch name? [Edge Case, Gap] → FR-013
- [x] CHK033 - Is behavior defined for worktrees on read-only file systems? [Edge Case, Gap] → FR-014
- [x] CHK034 - Is behavior defined for worktrees with symlinked directories? [Edge Case, Gap] → FR-015

---

## Error Handling Requirements

- [x] CHK035 - Are all FR-008 error reasons enumerated with required message content? [Completeness, FR-008]
- [x] CHK036 - Is the error message format specified (structured vs free text)? [Clarity, FR-008]
- [x] CHK037 - Are exit codes defined for different failure scenarios? [Gap] → CLI-005
- [x] CHK038 - Is the error reporting format for partial deletion failures specified? [Clarity, FR-010]
- [x] CHK039 - Are error messages for "worktree not found" vs "path not found" distinguished? [Clarity] → FR-011, FR-012

---

## CLI/UX Requirements

- [x] CHK040 - Is the command syntax explicitly specified (e.g., `wt remove <identifier>`)? [Completeness, Gap] → CLI-001
- [x] CHK041 - Is the output format specified for both success and failure cases? [Completeness, Gap] → CLI-003
- [x] CHK042 - Are JSON output requirements specified for programmatic consumption? [Completeness, Plan mentions JSON] → CLI-004
- [x] CHK043 - Is the help text content specified for the remove command? [Gap] → CLI-006
- [x] CHK044 - Are the flag names specified (`--force` vs `-f`)? [Completeness, Gap] → CLI-002
- [x] CHK045 - Is verbose output mode behavior defined? [Gap, Tasks T039 references --verbose] → CLI-002, NFR-002

---

## Non-Functional Requirements

- [x] CHK046 - Are performance requirements specified for removal operations? [NFR, Gap] → Defined in plan.md (sub-second execution)
- [x] CHK047 - Are cross-platform requirements explicitly stated (Windows/Linux/macOS differences)? [NFR, Plan mentions cross-platform] → Defined in plan.md
- [x] CHK048 - Are concurrent operation requirements defined (multiple wt processes)? [NFR, Gap] → NFR-001
- [x] CHK049 - Are logging/audit requirements specified for removal operations? [NFR, Gap] → NFR-002

---

## Dependencies & Assumptions

- [x] CHK050 - Is the assumption "permission to delete working directory" validated with fallback behavior? [Assumption, Spec Assumptions]
- [x] CHK051 - Is the dependency on git CLI explicitly documented with version requirements? [Dependency, Gap] → Out of Scope (project-level prerequisite)
- [x] CHK052 - Are the assumptions about "standard git worktree conventions" enumerated? [Assumption, Spec Assumptions]
- [x] CHK053 - Is the relationship to existing `create` and `list` commands documented? [Dependency, Consistency]

---

## Ambiguities & Conflicts

- [x] CHK054 - Is there ambiguity in what constitutes a "locked" worktree vs "has uncommitted changes"? [Ambiguity]
- [x] CHK055 - Does FR-010 (proceed with removal anyway) conflict with safety goals in FR-007? [Conflict]
- [x] CHK056 - Is the force flag behavior for "locked" vs "uncommitted" scenarios distinguished? [Ambiguity, FR-004 vs FR-007]
- [x] CHK057 - Is "active session" in FR-006 clearly distinguished from "currently inside directory"? [Ambiguity]

---

## Traceability

- [x] CHK058 - Do all functional requirements trace to at least one acceptance scenario? [Traceability]
- [x] CHK059 - Do all edge cases have corresponding functional requirements? [Traceability]
- [x] CHK060 - Do all success criteria have corresponding functional requirements? [Traceability]
- [x] CHK061 - Are clarification decisions traced to the requirements they modified? [Traceability, Clarifications §2026-01-24]

---

## Notes

- Check items off as completed: `[x]`
- Add inline comments for findings or concerns
- Reference specific spec sections when identifying issues
- Items marked `[Gap]` indicate missing requirements that should be addressed
- Items marked `[Ambiguity]` indicate unclear requirements needing clarification
- Items marked `[Conflict]` indicate potentially contradictory requirements

### Review Summary (2026-01-25)

All 61 checklist items have been reviewed and addressed:

- **Gaps resolved by adding new FRs**: FR-012 through FR-017
- **CLI requirements added**: CLI-001 through CLI-006 (command syntax, options, output formats, exit codes, help text)
- **NFRs added**: NFR-001 (concurrent operations), NFR-002 (logging)
- **Out of Scope clarified**: Git CLI version requirements (CHK051) - defined at project level
- **Clarifications added**: 10 new clarification entries documenting decisions

All items now pass. Specification is ready for implementation.
