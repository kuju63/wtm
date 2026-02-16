# Tasks: Release Binary Hash Files

**Input**: Design documents from `/specs/006-release-hash-files/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

**Note**: This is a CI/CD workflow feature. Tests are not applicable as the workflow execution itself serves as the test.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Verify prerequisites and understand existing implementation

- [ ] T001 Review current release workflow in .github/workflows/release.yml to understand existing hash generation
- [ ] T002 [P] Review existing hash generation script in .github/scripts/generate-checksums.sh
- [ ] T003 [P] Review existing release notes generation script in .github/scripts/generate-release-notes.sh
- [ ] T004 [P] Review research findings in specs/006-release-hash-files/research.md for format requirements
- [ ] T005 [P] Review data model in specs/006-release-hash-files/data-model.md for file format specifications

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure understanding - No dependencies blocking user stories for this CI/CD feature

**Note**: Since this is a workflow modification, there are no blocking foundational tasks. User stories can proceed directly.

**Checkpoint**: Foundation ready - user story implementation can begin

---

## Phase 3: User Story 1 - Automated Hash Generation on Release (Priority: P1) 🎯 MVP

**Goal**: Automatically generate individual `.sha256` hash files for all binary artifacts during release, in addition to the existing `SHA256SUMS` file.

**Independent Test**: Trigger release workflow and verify that individual `.sha256` files are generated for each binary and uploaded as release assets.

**Implementation Details** (from research.md & data-model.md):
- File format: `<hash>  <filename>` (two spaces separator)
- File naming: `<binary-filename>.sha256`
- Maintain backward compatibility with existing `SHA256SUMS` file

### Implementation for User Story 1

- [ ] T006 [P] [US1] Modify .github/scripts/generate-checksums.sh to generate individual .sha256 files in addition to SHA256SUMS
- [ ] T007 [P] [US1] Add input validation to .github/scripts/generate-checksums.sh to ensure all binaries exist before hash generation
- [ ] T008 [US1] Update .github/workflows/release.yml to upload individual .sha256 files in the "Create GitHub Release" step (update files: section)
- [ ] T009 [US1] Add error handling to .github/workflows/release.yml to fail workflow if .sha256 file generation fails (FR-005)
- [ ] T010 [P] [US1] Create .github/workflows/verify-hashes.yml workflow to test hash generation on pull requests
- [ ] T011 [US1] Test verify-hashes.yml workflow locally or in a test PR to ensure it validates hash file format and completeness

**Checkpoint**: Individual `.sha256` files should be generated and uploaded for each binary artifact in releases

---

## Phase 4: User Story 2 - User-Side Integrity Verification (Priority: P2)

**Goal**: Provide comprehensive Japanese documentation for users to verify downloaded binaries using hash files.

**Independent Test**: Review documentation and follow verification steps on each platform (Windows, Linux, macOS) to confirm instructions are clear and accurate.

**Implementation Details** (from quickstart.md):
- Platform-specific verification commands
- Troubleshooting guide
- FAQ section

### Implementation for User Story 2

- [ ] T012 [P] [US2] Copy specs/006-release-hash-files/quickstart.md to docs/release-verification.md
- [ ] T013 [P] [US2] Add "Verifying Downloads" section to docs/user-guide.md with link to release-verification.md
- [ ] T014 [US2] Update docs/user-guide.md with quick reference for hash verification commands (Windows, Linux, macOS)
- [ ] T015 [P] [US2] Add verification examples to docs/release-verification.md showing actual hash file content examples
- [ ] T016 [US2] Review and test documentation on all three platforms (Windows PowerShell, Linux sha256sum, macOS shasum)

**Checkpoint**: Users have clear, platform-specific documentation for verifying downloads

---

## Phase 5: User Story 3 - Hash Documentation in Release Notes (Priority: P3)

**Goal**: Display hash values directly in release notes in a user-friendly format, allowing quick verification without downloading separate hash files.

**Independent Test**: Create a test release and verify that hash values are displayed in release notes in the correct format (code block with download links).

**Implementation Details** (from research.md):
- Use Option A format: Code block with download links + verification instructions
- Include hash values for all binaries
- Provide verification commands for all platforms

### Implementation for User Story 3

- [ ] T017 [US3] Modify .github/scripts/generate-release-notes.sh to read SHA256SUMS file and extract hash values
- [ ] T018 [US3] Add "Checksums" section to release notes template in .github/scripts/generate-release-notes.sh
- [ ] T019 [US3] Include verification instructions for Windows, Linux, and macOS in release notes template
- [ ] T020 [US3] Format hash values as code block in release notes for easy copy-paste
- [ ] T021 [US3] Add download links to SHA256SUMS and individual .sha256 files in release notes
- [ ] T022 [US3] Test release notes generation locally to verify hash table formatting and links

**Checkpoint**: Release notes include checksums section with verification instructions

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and documentation improvements

- [ ] T023 [P] Add comments to .github/scripts/generate-checksums.sh explaining individual .sha256 file generation logic
- [ ] T024 [P] Add comments to .github/scripts/generate-release-notes.sh explaining hash table generation
- [ ] T025 [P] Update CLAUDE.md to document new hash file generation workflow (if needed)
- [ ] T026 [P] Add hash file generation example to project README or release documentation
- [ ] T027 Run end-to-end test: trigger release workflow and verify all hash files are generated and documented correctly
- [ ] T028 Validate hash file format compliance using regex from data-model.md (two spaces separator)
- [ ] T029 [P] Review all documentation for consistency and accuracy
- [ ] T030 Create ADR (Architecture Decision Record) documenting the decision to use GNU/Linux format and provide both SHA256SUMS and individual .sha256 files

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: N/A - No blocking foundational tasks for this CI/CD feature
- **User Stories (Phase 3-5)**: Can proceed independently after Setup (Phase 1) review
  - US1 (P1): Can start immediately after Setup
  - US2 (P2): Can start immediately after Setup (independent documentation task)
  - US3 (P3): Depends on US1 completion (needs .sha256 files to exist for release notes)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Setup - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Setup - Independent of US1 (documentation only)
- **User Story 3 (P3)**: Should start after US1 is complete (uses SHA256SUMS generated by US1)

### Within Each User Story

**US1 (Automated Hash Generation)**:
1. T006, T007 can run in parallel (both modify generate-checksums.sh)
2. T008 depends on T006 completion (needs .sha256 files to upload)
3. T009 depends on T008 (modifies same workflow file)
4. T010, T011 can run in parallel with T006-T009 (separate workflow file)

**US2 (User Documentation)**:
1. T012, T013, T015 can run in parallel (different files)
2. T014 depends on T013 (same file - user-guide.md)
3. T016 is final validation after all docs are written

**US3 (Release Notes Hash Table)**:
1. T017-T020 must run sequentially (all modify generate-release-notes.sh)
2. T021 depends on T020 (same script)
3. T022 is final testing

### Parallel Opportunities

**After Setup (Phase 1)**:
- US1 tasks T006 and T007 can run in parallel
- US1 task T010 can run in parallel with T006-T009
- US2 tasks T012, T013, T015 can run in parallel
- US2 entire phase can run in parallel with US1

**Within User Stories**:
- US1: T006, T007, T010 can all run in parallel (different concerns)
- US2: T012, T013, T015 can all run in parallel (different files)

**Across User Stories**:
- US1 and US2 can run completely in parallel (different files, independent goals)
- US3 should wait for US1 to complete (uses generated hash files)

---

## Parallel Example: User Story 1

```bash
# Launch parallel tasks for User Story 1:
Task T006: "Modify .github/scripts/generate-checksums.sh to generate individual .sha256 files"
Task T007: "Add input validation to .github/scripts/generate-checksums.sh"
Task T010: "Create .github/workflows/verify-hashes.yml workflow"

# Then sequentially:
Task T008: "Update .github/workflows/release.yml to upload .sha256 files"
Task T009: "Add error handling to release.yml"
Task T011: "Test verify-hashes.yml workflow"
```

## Parallel Example: User Story 2

```bash
# Launch all documentation tasks in parallel:
Task T012: "Copy quickstart.md to docs/release-verification.md"
Task T013: "Add 'Verifying Downloads' section to docs/user-guide.md"
Task T015: "Add verification examples to docs/release-verification.md"

# Then sequentially:
Task T014: "Update docs/user-guide.md with quick reference"
Task T016: "Review and test documentation on all platforms"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (review existing implementation)
2. Complete Phase 3: User Story 1 (automated hash generation)
3. **STOP and VALIDATE**: Test US1 by triggering a test release
4. Verify individual `.sha256` files are generated and uploaded correctly
5. Deploy/merge if ready

**Value**: Users can now verify downloads using individual `.sha256` files (basic integrity verification enabled)

### Incremental Delivery

1. **MVP (US1 only)**:
   - Individual hash files generated automatically ✅
   - Users can manually verify using `.sha256` files
   - Basic integrity protection in place

2. **Enhanced (US1 + US2)**:
   - Individual hash files generated ✅
   - **+ Comprehensive user documentation ✅**
   - Users have clear instructions for all platforms
   - Lower support burden (self-service verification)

3. **Complete (US1 + US2 + US3)**:
   - Individual hash files generated ✅
   - Comprehensive user documentation ✅
   - **+ Hash values in release notes ✅**
   - Maximum user convenience (no separate file download needed)
   - Best user experience

### Parallel Team Strategy

With multiple developers:

1. **Setup Phase**: Single developer reviews existing implementation (T001-T005)
2. **User Story Work** (can proceed in parallel):
   - **Developer A**: US1 (Hash Generation) - Priority P1
   - **Developer B**: US2 (Documentation) - Priority P2
   - US1 and US2 have NO dependencies and can run completely in parallel
3. **User Story 3**: After US1 completes
   - **Developer A or B**: US3 (Release Notes) - Priority P3
   - Depends on US1 completion (uses SHA256SUMS file)
4. **Polish Phase**: Team collaborates on final validation

---

## File Change Summary

### Modified Files

| File | User Story | Change Type | Description |
|------|-----------|-------------|-------------|
| `.github/scripts/generate-checksums.sh` | US1 | Modification | Add individual `.sha256` file generation |
| `.github/workflows/release.yml` | US1 | Modification | Upload `.sha256` files, add error handling |
| `.github/scripts/generate-release-notes.sh` | US3 | Modification | Add checksums section with verification instructions |
| `docs/user-guide.md` | US2 | Modification | Add "Verifying Downloads" section |

### New Files

| File | User Story | Description |
|------|-----------|-------------|
| `.github/workflows/verify-hashes.yml` | US1 | Test workflow for hash generation validation |
| `docs/release-verification.md` | US2 | Comprehensive hash verification guide (Japanese) |

---

## Success Criteria Mapping

Each task maps to specific success criteria from spec.md:

| Success Criterion | Tasks | Validation |
|------------------|-------|------------|
| **SC-001**: 100% of binaries have .sha256 files | T006-T011 | verify-hashes.yml workflow validates completeness |
| **SC-002**: Users can verify using standard tools | T012-T016 | Documentation tested on all platforms |
| **SC-003**: Workflow fails if hash generation fails | T009 | Error handling in release.yml |
| **SC-004**: Hash values visible in release notes | T017-T022 | Release notes include checksums section |
| **SC-005**: No support requests for verification | T012-T016, T017-T022 | Clear documentation + in-release-notes visibility |

---

## Notes

- **[P] tasks**: Different files, no dependencies - safe to run in parallel
- **[Story] label**: Maps task to specific user story for traceability
- **No test tasks**: Workflow execution IS the test - verify-hashes.yml workflow serves this purpose
- **Backward compatibility**: Maintain existing `SHA256SUMS` file and `SHA256SUMS.asc` signature
- **Format validation**: All `.sha256` files must use GNU/Linux format (`<hash>  <filename>` with two spaces)
- **Error handling**: Workflow MUST fail if any hash file generation fails (FR-005)
- **Documentation language**: User-facing docs in Japanese, code comments in English
- **Commit strategy**: Commit after each user story phase completion
- **MVP scope**: User Story 1 only is sufficient for MVP (enables basic integrity verification)
