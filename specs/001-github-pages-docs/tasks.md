# Tasks: GitHub Pages Documentation Publishing

**Input**: Design documents from `/specs/001-github-pages-docs/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: No xUnit tests required - documentation validated through build process (DocFX + LinkChecker)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Project follows single repository structure:

- Documentation source: `docs/`
- CLI project: `wt.cli/`
- Tools: `Tools/DocGenerator/`
- GitHub Actions: `.github/workflows/`
- Build output: `_site/` (not committed)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Enable XML documentation and DocFX configuration

- [x] T001 Enable XML documentation generation in wt.cli/wt.cli.csproj (add GenerateDocumentationFile and DocumentationFile properties)
- [x] T002 [P] Verify docfx.json configuration exists and includes API metadata generation settings
- [x] T003 [P] Create Tools/DocGenerator/ directory and initialize console project with System.CommandLine package

**Checkpoint**: XML docs enabled, DocFX configured, DocGenerator project initialized

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core documentation infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Implement command documentation generator in Tools/DocGenerator/Program.cs with MarkdownConsole and CommandDocGenerator classes
- [x] T005 Add ConvertCommandToMarkdown method to Tools/DocGenerator/Program.cs for System.CommandLine help text extraction
- [x] T006 [P] Integrate documentation build into .github/workflows/release.yml as build-and-deploy-docs job (runs after create-release job completes)
- [x] T007 [P] Add DocFX installation with caching to .github/workflows/release.yml (dotnet tool install docfx --version ${{ env.DOCFX_VERSION }} --global)
- [x] T008 Add version extraction logic to .github/workflows/release.yml (extract minor version from needs.calculate-version.outputs.version)
- [x] T009 Create .github/scripts/update-version-manifest.py with positional arguments <manifest_path> <version> and automatic publishedDate generation
- [x] T010 [P] Configure GitHub Pages deployment in .github/workflows/release.yml using peaceiris/actions-gh-pages@v4 with keep_files: true
- [x] T011 Add XML documentation comments to all public classes, methods, and properties in wt.cli/ (per FR-015 and SC-006)

**Implementation Note**: Originally designed as separate .github/workflows/docs.yml triggered by release.published event, but consolidated into release.yml to avoid GitHub Actions limitation where GITHUB_TOKEN cannot trigger other workflows. This ensures documentation is automatically built and deployed as part of the release process.

**Checkpoint**: Foundation ready - documentation can be generated and deployed automatically

---

## Phase 3: User Story 1 - Quick Start Installation (Priority: P1) 🎯 MVP

**Goal**: New users can install the tool and start using it within 5 minutes (SC-001)

**Independent Test**: A new user follows docs/installation.md from start to finish, installs the binary, and successfully runs `wt --version`

### Implementation for User Story 1

- [x] T012 [US1] Create docs/installation.md with system requirements section (Windows 10+, macOS 11+, Linux x64/ARM)
- [x] T013 [US1] Add download section to docs/installation.md with links to release assets for all platforms (wt-win-x64.zip, wt-osx-arm64.tar.gz, wt-linux-x64.tar.gz, wt-linux-arm.tar.gz)
- [x] T014 [P] [US1] Add Windows installation instructions to docs/installation.md (extract, move to Program Files, add to PATH, verify)
- [x] T015 [P] [US1] Add macOS installation instructions to docs/installation.md (extract, move to /usr/local/bin, chmod +x, verify)
- [x] T016 [P] [US1] Add Linux installation instructions to docs/installation.md (extract, move to /usr/local/bin, chmod +x, verify)
- [x] T017 [US1] Add troubleshooting section to docs/installation.md (command not found, permission denied, Git not found)
- [x] T018 [US1] Create docs/guides/quickstart.md with first steps tutorial (create first worktree, basic commands)
- [x] T019 [US1] Update docs/index.md homepage with prominent Installation Guide link and Quick Start call-to-action
- [x] T020 [US1] Update docs/toc.yml to include installation and quickstart in navigation structure

**Checkpoint**: Installation documentation complete - users can install and verify the tool independently

---

## Phase 4: User Story 2 - Command Reference Lookup (Priority: P1)

**Goal**: Users can find detailed command documentation within 30 seconds (SC-002)

**Independent Test**: A user navigates to Command Reference, searches for a command, and finds complete syntax, parameters, and examples

### Implementation for User Story 2

- [x] T021 [US2] Run Tools/DocGenerator to generate command documentation markdown files in docs/commands/ directory (auto-generated from System.CommandLine definitions)
- [x] T022 [US2] Create docs/commands/index.md with command overview and category organization
- [x] T023 [P] [US2] Add command examples to docs/commands/create.md for 'create' command (default path, custom path, editor integration)
- [x] T024 [P] [US2] Add command examples to docs/commands/list.md for 'list' command (all worktrees, filtered by branch)
- [x] T025 [P] [US2] Add command examples for remaining commands (manual documentation created)
- [x] T026 [US2] Update docs/toc.yml to include commands section with auto-generated command list
- [x] T027 [US2] Add search functionality configuration to docfx.json (enable DocFX search plugin)
- [x] T028 [US2] Update index.md to include Command Reference section link with search call-to-action
- [x] T029 [US2] Add DocGenerator execution step to .github/workflows/release.yml before DocFX build (dotnet run --project Tools/DocGenerator -- --output docs/)

**Checkpoint**: Command reference complete - all commands documented with examples and searchable

---

## Phase 5: User Story 3 - Version-Specific Documentation Access (Priority: P2)

**Goal**: Users can switch between minor versions to see accurate documentation for their installed version (SC-004, FR-006, FR-007)

**Independent Test**: A user selects a different version from the version switcher, the page reloads showing that version's documentation with updated URL

### Implementation for User Story 3

- [ ] T030 [US3] Create version-switcher.js JavaScript module with version manifest fetching and dropdown population logic
- [ ] T031 [US3] Add version detection logic to version-switcher.js (extract version from URL path /v{major}.{minor}/)
- [ ] T032 [US3] Add version navigation logic to version-switcher.js (preserve current page path when switching versions)
- [ ] T033 [US3] Create version-switcher.css stylesheet with dropdown styling and current version indicator
- [ ] T034 [US3] Add version switcher HTML to docfx.json template overrides (inject into page header)
- [ ] T035 [US3] Update .github/scripts/update-version-manifest.py to handle first release case (peaceiris action handles existing manifest fetching)
- [ ] T036 [US3] Add new version entry logic to .github/scripts/update-version-manifest.py (update isLatest flags, add new version, sort by version)
- [ ] T037 [US3] Add manifest validation to .github/scripts/update-version-manifest.py (ensure one isLatest, valid JSON schema)
- [x] T038 [US3] Add version-specific directory structure to .github/workflows/release.yml (build to _site/v{major}.{minor}/)
- [x] T039 [US3] Add manifest update step to .github/workflows/release.yml (python .github/scripts/update-version-manifest.py)
- [x] T040 [US3] Add manifest deployment step to .github/workflows/release.yml (copy to root and version directory)
- [ ] T041 [US3] [FR-010] Create redirect page at root (_site/index.html) to automatically redirect to latest version from versions.json
- [ ] T042 [US3] Add version indicator to page footer in docfx.json template (display current version on every page per FR-007)

**Checkpoint**: Version switching functional - users can access documentation for any published minor version

---

## Phase 6: User Story 4 - API Reference for Developers (Priority: P2)

**Goal**: Developers can find comprehensive API documentation for all public interfaces (SC-006, FR-005, FR-015)

**Independent Test**: A developer navigates to API Reference, finds a specific class, sees method signatures with XML comments, and successfully integrates it

### Implementation for User Story 4

- [ ] T043 [US4] Verify wt.cli.csproj has NoWarn configuration to suppress 1591 warnings during development
- [ ] T044 [US4] Configure docfx.json metadata section to extract API documentation from wt.cli XML output
- [ ] T045 [US4] Add API reference section to docs/toc.yml with automatic API navigation structure
- [ ] T046 [US4] Create docs/api/index.md overview page with API integration patterns and examples
- [ ] T047 [US4] Add xref configuration to docfx.json for .NET framework cross-references
- [ ] T048 [US4] Add DocFX metadata generation step to .github/workflows/release.yml (docfx metadata)
- [ ] T049 [US4] Add API documentation build step to .github/workflows/release.yml (docfx build with API metadata)
- [ ] T050 [US4] Update docs/index.md to include API Reference section link for developers

**Checkpoint**: API documentation complete - 100% of public APIs documented and accessible

---

## Phase 7: User Story 5 - Contributing to the Project (Priority: P3)

**Goal**: Contributors can set up development environment with 95% success rate (SC-005, FR-004)

**Independent Test**: A new contributor follows docs/contributing.md, sets up environment, runs tests, and submits a PR

### Implementation for User Story 5

- [ ] T051 [US5] Create docs/contributing.md with development environment setup section (prerequisites: .NET 10 SDK, Git)
- [ ] T052 [US5] Add setup steps to docs/contributing.md (clone, dotnet restore, dotnet build, dotnet test, dotnet run)
- [ ] T053 [US5] Add coding standards section to docs/contributing.md with link to .specify/memory/constitution.md
- [ ] T054 [US5] Add TDD workflow section to docs/contributing.md (write tests first, implement, refactor)
- [ ] T055 [US5] Add pull request process to docs/contributing.md (create branch, write tests, implement, submit PR)
- [ ] T056 [US5] Add issue reporting section to docs/contributing.md (bug report template, feature request template)
- [ ] T057 [US5] Add note distinguishing user installation (no .NET) vs developer setup (.NET SDK required)
- [ ] T058 [US5] Update docs/toc.yml to include contributing guide in navigation
- [ ] T059 [US5] Update docs/index.md to include Contributing section link

**Checkpoint**: Contribution guide complete - new contributors can set up and contribute successfully

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Quality improvements and validation

- [ ] T060 [P] Add LinkChecker step to .github/workflows/release.yml for link validation (check internal and external links)
- [ ] T061 [P] Add DocFX build validation with --warningsAsErrors flag to .github/workflows/release.yml
- [ ] T062 [P] Create docs/ja/ directory with Japanese translations for key documentation pages (installation, command overview)
- [ ] T063 [P] Add CODE_OF_CONDUCT.md link to docs/contributing.md
- [ ] T064 Add performance optimization to docfx.json (enable caching, optimize asset loading per SC-007)
- [ ] T065 Add 404 error page to docs/404.md with navigation back to latest version
- [ ] T066 Test complete workflow end-to-end with mock release event
- [ ] T067 Validate documentation loads within 2 seconds on standard broadband (SC-007)
- [ ] T068 Verify all edge cases from spec.md (non-existent version, JavaScript disabled, deprecated versions)
- [ ] T069 Run quickstart.md validation to ensure 4-6 hour implementation estimate is accurate

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - User stories can proceed in parallel if team capacity allows
  - Or sequentially in priority order: US1 → US2 → US3 → US4 → US5
- **Polish (Phase 8)**: Depends on desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - Independent
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Independent
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Enhances US1 and US2 but independently testable
- **User Story 4 (P2)**: Can start after Foundational (Phase 2) - Independent
- **User Story 5 (P3)**: Can start after Foundational (Phase 2) - Independent

### Within Each User Story

- Tasks should be completed in order unless marked [P]
- User story is complete when checkpoint criteria is met
- Each story delivers value independently

### Parallel Opportunities

- **Phase 1**: T002, T003 can run in parallel
- **Phase 2**: T006, T007, T010 can run in parallel
- **Phase 3 (US1)**: T014, T015, T016 can run in parallel (different platform sections)
- **Phase 4 (US2)**: T023, T024, T025 can run in parallel (different command examples)
- **Phase 8**: T060, T061, T062, T063 can run in parallel

Once Foundational phase completes:

- **All 5 user stories can be worked on in parallel by different team members**
- Each story is independently completable and testable

---

## Parallel Example: User Story 1 (Installation Guide)

```bash
# Launch all platform-specific sections together:
Task T014: "Add Windows installation instructions to docs/installation.md"
Task T015: "Add macOS installation instructions to docs/installation.md"  
Task T016: "Add Linux installation instructions to docs/installation.md"

# These can be written simultaneously by different team members
# Each section is independent and targets different file regions
```

---

## Parallel Example: User Story 2 (Command Reference)

```bash
# Launch all command example tasks together:
Task T023: "Add command examples for 'create' command"
Task T024: "Add command examples for 'list' command"
Task T025: "Add command examples for remaining commands"

# All modify Tools/DocGenerator/Program.cs but in different methods
# Can be developed in parallel with good coordination
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete Phase 2: Foundational (T004-T011) - **CRITICAL BLOCKER**
3. Complete Phase 3: User Story 1 (T012-T020) - Installation Guide
4. Complete Phase 4: User Story 2 (T021-T029) - Command Reference
5. **STOP and VALIDATE**: Test installation and command lookup independently
6. Deploy to GitHub Pages and validate with real users
7. This is the **minimum viable documentation** (SC-001, SC-002 met)

### Incremental Delivery

1. **Foundation** (Phases 1-2) → Documentation infrastructure ready
2. **MVP** (Phases 3-4) → Installation + Commands → Deploy (satisfies P1 requirements)
3. **Enhanced** (+Phase 5) → Add version switching → Deploy (satisfies FR-006, FR-007)
4. **Developer Support** (+Phase 6) → Add API docs → Deploy (satisfies FR-005, SC-006)
5. **Community** (+Phase 7) → Add contribution guide → Deploy (satisfies FR-004, SC-005)
6. **Polish** (Phase 8) → Quality improvements → Final deployment

### Parallel Team Strategy

With multiple developers:

1. **Team completes Setup + Foundational together** (critical path)
2. **Once Foundational is done** (after T011):
   - Developer A: User Story 1 (Installation Guide)
   - Developer B: User Story 2 (Command Reference)
   - Developer C: User Story 3 (Version Switching)
   - Developer D: User Story 4 (API Reference)
   - Developer E: User Story 5 (Contribution Guide)
3. Stories complete independently and integrate via GitHub Pages deployment

### Single Developer Strategy

Follow sequential priority order:

1. Setup → Foundational → US1 → US2 → US3 → US4 → US5 → Polish
2. Deploy after US1+US2 for early user feedback
3. Deploy again after each additional story
4. Estimated total time: 4-6 hours for US1+US2, +1-2 hours per additional story

---

## Notes

- **No xUnit tests**: Documentation validated through DocFX build and LinkChecker
- **Auto-generation**: Command docs and API docs generated automatically (T004-T005, T021, T048-T049)
- **Version management**: Python script handles manifest updates (T009, T035-T037)
- **Deployment**: GitHub Actions orchestrates entire workflow (T006-T010, T029, T038-T041, T048-T049, T060-T061)
- **Constitution compliance**: All gates passed (see plan.md Constitution Check section)
- **Performance**: Static site architecture ensures SC-007 (2-second load time)
- **Coverage**: API docs cover 100% public APIs per SC-006
- **Automation**: Documentation publishes within 10 minutes per SC-003

**Total Tasks**: 69 tasks organized into 8 phases (5 user stories + setup/foundational/polish)

**MVP Scope**: Phases 1-4 (32 tasks) deliver minimum viable documentation
**Full Scope**: All 8 phases (69 tasks) deliver complete documentation system

**Estimated Time**:

- MVP (US1+US2): 4-6 hours
- Enhanced (+US3): +1-2 hours  
- Developer (+US4): +1 hour
- Community (+US5): +1 hour
- Polish: +1 hour
- **Total**: 8-11 hours for complete feature

**Parallel Opportunities**: 15+ tasks can run in parallel, reducing total time by 30-40% with multiple developers
