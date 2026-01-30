# Implementation Plan: GitHub Pages Documentation Publishing

**Branch**: `001-github-pages-docs` | **Date**: 2026-01-15 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-github-pages-docs/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Automate the publishing of comprehensive documentation to GitHub Pages synchronized with binary releases. The documentation includes Installation Guide, Command Reference, API Reference, and Contribution Guide with version-specific access at the minor version level (v1.0, v1.1, etc.). The system will use DocFX (already configured) to generate static documentation from markdown files and XML documentation comments, deployed via GitHub Actions to GitHub Pages whenever a new release is created.

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**:

- DocFX 2.78.4 (documentation generator, .NET global tool)
- System.CommandLine 2.0.2 (for CLI command documentation extraction)
- GitHub Actions (for CI/CD automation)
- GitHub Pages (for hosting)
**Storage**: Static files (markdown, generated HTML/CSS/JS)  
**Testing**:
- DocFX build validation with `--warningsAsErrors` flag
- LinkChecker for link validation  
**Target Platform**: GitHub Pages static hosting (cross-platform access via web browser)
**Project Type**: Single project with documentation output (static site generator workflow)  
**Performance Goals**:
- Documentation site loads within 2 seconds on standard broadband (per SC-007)
- Build and publish within 10 minutes of release (per SC-003)
**Constraints**:
- Version-specific URLs must remain stable for 2+ years (architectural constraint via GitHub Pages)
- Must maintain documentation for each minor version independently (per FR-011)
- No backend/database - pure static site architecture
**Scale/Scope**:
- Initial: 5-10 documentation pages per version
- API Reference: Generated from all public APIs in wt.cli project
- Estimated versions to support simultaneously: 3-5 minor versions
- Documentation format: Markdown source → DocFX → Static HTML

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Initial Check (Pre-Phase 0)

#### I. Developer Usability (CLI優先)

✅ **PASS** - Documentation enhances CLI usability by providing comprehensive command reference and examples. This feature supports the CLI-first approach rather than replacing it.

#### II. Cross-Platform (クロスプラットフォーム対応)

✅ **PASS** - GitHub Pages documentation is accessible from any platform via web browser. DocFX-generated sites are pure HTML/CSS/JS with no platform dependencies.

#### III. Clean & Secure Code (クリーンでセキュアなコード)

✅ **PASS** - Static documentation poses minimal security risk. No secrets will be embedded in documentation. GitHub Actions workflow will use repository secrets for deployment tokens. All generated content is public-facing and contains no sensitive information.

#### IV. Documentation Clarity (ドキュメントの明瞭性)

✅ **PASS** - **This feature directly implements this principle.** Documentation will be primarily in Japanese (as specified in constitution) with technical terms in English. Technical decisions about documentation architecture will be recorded as ADRs.

#### V. Minimal Dependencies (最小限の依存関係)

⚠️ **REQUIRES JUSTIFICATION** - Adding DocFX as a documentation dependency.

- **Justification**: DocFX is specifically designed for .NET projects and can generate API documentation from XML comments. Alternatives like Sphinx (Python-focused), MkDocs (lacks API generation for .NET), or custom solution (high maintenance burden) are less suitable.
- **Risk Mitigation**: DocFX is maintained by Microsoft/.NET Foundation, reducing supply chain risk. We will pin specific versions and monitor for vulnerabilities.

#### VI. Comprehensive Testing (テストの充実と自動化)

✅ **PASS (RESOLVED)** - Testing strategy defined in research.md:

- Layer 1: DocFX build-time validation with `--warningsAsErrors`
- Layer 2: LinkChecker for link validation
- No xUnit tests required for static documentation content
- Command documentation generation is part of build pipeline (fails if commands missing)

#### VII. Quantitative Thresholds (閾値)

✅ **PASS** - Documentation build workflow is automation code, not production code. Standard code quality thresholds apply to any custom scripts. Documentation content itself follows clarity and completeness metrics defined in success criteria (SC-001 through SC-007).

### Post-Design Re-Evaluation (After Phase 1)

#### I. Developer Usability (CLI優先)

✅ **CONFIRMED** - Design maintains CLI-first focus:

- Command documentation auto-generated from CLI definitions
- No separate documentation maintenance burden
- Users access docs via web (universal accessibility)

#### II. Cross-Platform (クロスプラットフォーム対応)

✅ **CONFIRMED** - Implementation is platform-agnostic:

- GitHub Pages works on all browsers
- No platform-specific deployment requirements
- DocFX builds on ubuntu-latest runner (Linux)

#### III. Clean & Secure Code (クリーンでセキュアなコード)

✅ **CONFIRMED** - Security measures implemented:

- OIDC authentication (no long-lived tokens)
- Minimal permissions (`contents: write`, `pages: write`, `id-token: write`)
- Python manifest script validates JSON without arbitrary code execution
- No secrets exposed in logs or deployed content

#### IV. Documentation Clarity (ドキュメントの明瞭性)

✅ **CONFIRMED** - Documentation structure supports clarity:

- Installation guide explains no .NET/Git required for end users
- Contribution guide clarifies .NET SDK required for developers only
- Clear separation of user vs. developer documentation
- Version-specific documentation prevents confusion

#### V. Minimal Dependencies (最小限の依存関係)

✅ **JUSTIFIED** - Dependency analysis post-design:

- DocFX 2.78.4: Pinned version, Microsoft/.NET Foundation maintained
- LinkChecker: Build-time only, not runtime dependency
- Python 3: Pre-installed on GitHub runners, no additional installation
- System.CommandLine: Already used in wt.cli, no new dependency
- **Total new dependencies**: 1 (DocFX) + 1 build-time (LinkChecker)
- **Risk level**: LOW - Both are well-maintained tools with established ecosystems

#### VI. Comprehensive Testing (テストの充実と自動化)

✅ **CONFIRMED** - Two-layer testing strategy validated:

- Build validation catches markdown errors, broken internal links
- Link validation catches broken external links, missing resources
- Command documentation generation failures caught in build pipeline
- Manual testing not required due to automation
- **No TDD required**: Documentation is build artifact, not executable code

#### VII. Quantitative Thresholds (閾値)

✅ **CONFIRMED** - Custom scripts meet quality standards:

- `Tools/DocGenerator/Program.cs`: Will follow standard C# code quality rules
- `.github/scripts/update-version-manifest.py`: Simple script (<100 LOC), single responsibility
- GitHub Actions YAML: Declarative configuration, no complex logic
- **All scripts subject to PR review and linting**

### Final Verdict

**✅ ALL GATES PASSED**

**Justifications Accepted**:

1. DocFX dependency justified due to .NET-specific API documentation generation needs
2. No xUnit tests for static documentation content (validated through build process)

**Action Items for Implementation**:

- Pin DocFX version to 2.78.4 in workflow
- Ensure Python manifest script has input validation
- Add linting for Python script (optional: pylint or black)
- Document dependency justification in ADR (if constitutional amendment process requires)

**Ready to proceed to Phase 2 (Task Generation)**

## Project Structure

### Documentation (this feature)

```text
specs/005-github-pages-docs/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── github-workflow-schema.yml  # GitHub Actions workflow contract
│   └── docfx-config-schema.json    # DocFX configuration schema
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
wt/ (repository root)
├── .github/
│   └── workflows/
│       ├── release.yml          # [MODIFY] Add docs deployment step
│       └── docs.yml              # [NEW] Dedicated docs build/deploy workflow
├── docs/                         # [EXISTING] Documentation source
│   ├── index.md                  # [MODIFY] Enhanced homepage
│   ├── installation.md           # [NEW] Installation guide
│   ├── commands/                 # [NEW] Command reference section
│   │   ├── index.md              # Command overview
│   │   ├── create.md             # wt create documentation
│   │   └── list.md               # wt list documentation
│   ├── api/                      # [AUTO-GENERATED] API reference
│   ├── contributing.md           # [NEW] Contribution guide
│   ├── guides/                   # [EXISTING] Extended with new content
│   │   └── quickstart.md         # [NEW] Quick start guide
│   ├── ja/                       # [EXISTING] Japanese versions
│   │   ├── index.md
│   │   ├── installation.md
│   │   └── ...
│   └── toc.yml                   # [MODIFY] Table of contents
├── wt.cli/
│   └── wt.cli.csproj             # [VERIFY] XML documentation enabled
├── docfx.json                    # [EXISTING] DocFX configuration
├── index.md                      # [EXISTING] Root index
└── toc.yml                       # [EXISTING] Root ToC

Build Output Structure:
_site/                            # DocFX build output
├── v1.0/                         # Version-specific docs
│   ├── index.html
│   ├── installation.html
│   ├── commands/
│   ├── api/
│   └── ...
├── v1.1/                         # Next version
│   └── ...
└── index.html                    # Latest version redirect
```

**Structure Decision**: Using single project structure with DocFX documentation generator. The existing `docs/` directory structure is preserved and extended. DocFX generates static HTML into `_site/` directory which is deployed to GitHub Pages. Version-specific documentation is organized by creating separate build outputs for each minor version (v1.0/, v1.1/, etc.) within the GitHub Pages branch (gh-pages).

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Adding DocFX dependency | DocFX provides .NET-native API documentation generation from XML comments and markdown processing with versioning support | MkDocs: Cannot generate .NET API docs from XML; Sphinx: Python-focused, poor .NET integration; Custom HTML: High maintenance burden and no API extraction capability |

---

## Implementation Summary

### Artifacts Generated (Phase 1 Complete)

✅ **Research Phase (Phase 0)**:

- `research.md` - Comprehensive research on DocFX, testing strategies, version switching, and automation

✅ **Design Phase (Phase 1)**:

- `data-model.md` - Entity definitions and relationships for documentation system
- `contracts/github-workflow-contract.md` - GitHub Actions workflow specification
- `contracts/docfx-config-contract.md` - DocFX configuration specification
- `quickstart.md` - Step-by-step implementation guide (4-6 hours estimated)

✅ **Agent Context**:

- `AGENTS.md` - Updated with documentation automation guidance

### Key Decisions

1. **DocFX 2.78.4**: Selected for .NET 10 support and native API documentation generation
2. **Command Automation**: System.CommandLine HelpBuilder for programmatic documentation export
3. **Testing Strategy**: Two-layer approach (build validation + link checking)
4. **Version Management**: Python script auto-updates manifest per release
5. **No xUnit Tests**: Static documentation validated through build process, not unit tests

### Dependencies Added

| Dependency | Version | Type | Justification |
|------------|---------|------|---------------|
| DocFX | 2.78.4 | Build tool | .NET-native API doc generation, Microsoft/.NET Foundation maintained |
| LinkChecker | latest | Build tool | Link validation in CI/CD pipeline |
| Python 3 | 3.x | Runtime | Pre-installed on GitHub runners, manifest management |

### Architecture Highlights

- **Versioned Structure**: `/v{major}.{minor}/` directories for each version
- **Version Switcher**: JavaScript-based UI with `version-manifest.json` backend
- **Automated Pipeline**: Release trigger → Doc generation → Build → Validate → Deploy
- **Zero Manual Maintenance**: Command docs auto-generated from CLI code

### Success Criteria Mapping

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| FR-001 (Auto-publish on release) | GitHub Actions workflow triggered on `release.published` | ✅ Designed |
| FR-002 (Installation guide) | `docs/installation.md` with platform-specific instructions | ✅ Designed |
| FR-003 (Command reference) | Auto-generated from System.CommandLine via `Tools/DocGenerator` | ✅ Designed |
| FR-006 (Version switching) | JavaScript switcher + version-manifest.json | ✅ Designed |
| SC-003 (10 min publish) | Estimated 5-8 min workflow duration | ✅ Designed |
| SC-007 (2 sec load time) | Static HTML, no backend, CDN via GitHub Pages | ✅ Designed |

### Ready for Phase 2

**All Phase 1 deliverables complete. Proceeding to Phase 2 (Task Generation) is approved.**

**Branch**: `001-github-pages-docs`  
**Next Command**: `/speckit.tasks` to generate implementation tasks from this plan

---

**Plan Status**: ✅ Complete  
**Last Updated**: 2026-01-16  
**Reviewed By**: AI Planning Agent  
**Approval**: Ready for implementation
