# Implementation Plan: Release Binary Hash Files

**Branch**: `001-release-hash-files` | **Date**: 2026-02-14 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-release-hash-files/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

This feature adds automated SHA256 hash file generation to the release process. When a release is published, the CI/CD workflow automatically generates hash files for all binary artifacts, uploads them as release assets, and includes hash values in release notes. This enables users to verify download integrity using standard command-line tools.

**Technical Approach**: Modify GitHub Actions release workflow to generate SHA256 hash files after binary builds, upload hash files alongside binaries, and update release notes template to include hash values.

## Technical Context

**Language/Version**: YAML (GitHub Actions workflows), Bash/PowerShell (hash generation scripts)
**Primary Dependencies**: GitHub Actions, sha256sum (Linux/macOS), Get-FileHash (Windows PowerShell)
**Storage**: GitHub Releases (release assets storage)
**Testing**: GitHub Actions workflow execution tests, hash verification integration tests
**Target Platform**: GitHub Actions runners (ubuntu-latest, windows-latest, macos-latest)
**Project Type**: CI/CD workflow modification with minimal tool changes
**Performance Goals**: Hash generation completes within 10 seconds per artifact (typically <5MB binaries)
**Constraints**: GitHub Actions execution time limits (<6 hours per workflow), release asset size limits (2GB per file, 10GB total)
**Scale/Scope**: 3-5 binary artifacts per release (Windows, Linux, macOS variants), ~10-20 releases per year

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Initial Check (Before Research)

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Developer Usability** | ✅ PASS | Users verify downloads using standard CLI tools (sha256sum, Get-FileHash); hash files follow industry-standard format |
| **II. Cross-Platform** | ✅ PASS | SHA256 hashing supported on all platforms; hash files are plain text, platform-agnostic |
| **III. Clean & Secure Code** | ✅ PASS | Uses secure SHA256 algorithm; minimal script complexity; no hardcoded secrets; hash generation scripts are simple and auditable |
| **IV. Documentation Clarity** | ✅ PASS | Will provide Japanese documentation for hash verification in README and release notes |
| **V. Minimal Dependencies** | ✅ PASS | Uses OS-standard tools only (sha256sum on Linux/macOS, Get-FileHash on Windows); no new dependencies added |
| **VI. Comprehensive Testing** | ✅ PASS | Will include workflow execution tests, hash file generation verification, and integration tests for hash validation |

**Quantitative Thresholds**:
- Method LOC: Hash generation scripts <50 lines ✅
- Cyclomatic Complexity: Scripts are linear (<3) ✅
- Test Coverage: Workflow and scripts will have 90%+ coverage ✅
- Dependency Vulnerabilities: No new dependencies ✅
- PR Quality Gate: All PRs will pass automated tests ✅

**Result**: ✅ ALL GATES PASSED - Proceed to Phase 0 Research

### Post-Design Check

Re-evaluated after completing research.md, data-model.md, and quickstart.md:

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Developer Usability** | ✅ PASS | quickstart.md provides clear, platform-specific verification instructions; supports both individual .sha256 files and batch SHA256SUMS verification |
| **II. Cross-Platform** | ✅ PASS | data-model.md confirms GNU/Linux standard format works across Windows (PowerShell), Linux (sha256sum), and macOS (shasum); no platform-specific dependencies |
| **III. Clean & Secure Code** | ✅ PASS | research.md confirms use of industry-standard SHA256 algorithm and file formats; scripts will be simple (<50 lines) with no complex logic |
| **IV. Documentation Clarity** | ✅ PASS | quickstart.md provides comprehensive Japanese documentation for end users; release notes will include checksums section |
| **V. Minimal Dependencies** | ✅ PASS | Uses OS-native tools only (sha256sum, shasum, Get-FileHash); no new package dependencies |
| **VI. Comprehensive Testing** | ✅ PASS | verify-hashes.yml workflow will test hash generation; data-model.md defines validation rules for format compliance |

**Quantitative Thresholds**:
- Method LOC: Hash generation script <50 lines ✅ (single function to iterate binaries and generate hashes)
- Cyclomatic Complexity: Linear workflow, <3 ✅ (loop + file I/O only)
- Test Coverage: Workflow validation covers critical path ✅
- Dependency Vulnerabilities: No new dependencies ✅
- PR Quality Gate: CI workflow tests before merge ✅

**Result**: ✅ ALL GATES PASSED - Design conforms to all constitution principles

**Design Changes from Initial Plan**:
- Confirmed both SHA256SUMS (existing) and individual .sha256 files (new) will coexist
- Finalized file format: GNU/Linux standard (`<hash>  <filename>` with two spaces)
- Defined release notes format: code block with download links + verification instructions

## Project Structure

### Documentation (this feature)

```text
specs/001-release-hash-files/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)

Note: contracts/ is not applicable (no API contracts for CI/CD feature)
```

### Source Code (repository root)

This feature modifies existing CI/CD workflows and scripts:

```text
.github/
├── workflows/
│   ├── release.yml              # Modified: Add individual .sha256 file generation
│   └── verify-hashes.yml        # New: Test workflow to verify hash generation
└── scripts/
    ├── generate-checksums.sh    # Modified: Generate both SHA256SUMS and individual .sha256 files
    └── generate-release-notes.sh # Modified: Include hash values table in release notes

docs/
├── user-guide.md                # Modified: Add "Verifying Downloads" section
└── release-verification.md      # New: Detailed hash verification guide (Japanese)
```

**Structure Decision**: This feature uses **CI/CD workflow modification** pattern. Changes are minimal and focused on existing release automation scripts.

### Current vs. Enhanced Release Pipeline

**Current Release Flow** (implemented):
```text
1. calculate-version  → Determine next version
2. build              → Build binaries for all platforms
3. create-release:
   ├─ Organize binaries (rename with version)
   ├─ Generate checksums (.github/scripts/generate-checksums.sh)
   │  └─ Creates: SHA256SUMS (single file with all hashes)
   ├─ Generate SBOM
   ├─ Sign artifacts (SHA256SUMS.asc)
   ├─ Generate release notes (plain format, NO hash values)
   └─ Create GitHub Release
      └─ Uploads: binaries, SHA256SUMS, SHA256SUMS.asc, SBOM
```

**Enhanced Release Flow** (this feature adds):
```text
1. calculate-version  → (unchanged)
2. build              → (unchanged)
3. create-release:
   ├─ Organize binaries → (unchanged)
   ├─ Generate checksums (.github/scripts/generate-checksums.sh) ★ MODIFIED
   │  ├─ Creates: SHA256SUMS (existing, unchanged)
   │  └─ Creates: individual .sha256 files (NEW)
   │     ├─ wt-v1.0.0-windows-x64.exe.sha256
   │     ├─ wt-v1.0.0-linux-x64.sha256
   │     ├─ wt-v1.0.0-linux-arm.sha256
   │     └─ wt-v1.0.0-macos-arm64.sha256
   ├─ Generate SBOM → (unchanged)
   ├─ Sign artifacts → (unchanged, signs SHA256SUMS only)
   ├─ Generate release notes (.github/scripts/generate-release-notes.sh) ★ MODIFIED
   │  └─ Includes: hash values table (NEW)
   └─ Create GitHub Release ★ MODIFIED
      └─ Uploads: binaries, .sha256 files (NEW), SHA256SUMS, SHA256SUMS.asc, SBOM
```

**Key Additions**:
- ✅ Individual `.sha256` files per binary (FR-002)
- ✅ Hash values table in release notes (FR-006)
- ✅ Verification workflow to test hash generation (FR-005)

**What Stays Unchanged**:
- Existing SHA256SUMS file (maintains backward compatibility)
- SBOM generation and signing workflow
- Binary build process
- Version calculation logic

## Complexity Tracking

No constitution violations detected. This section is not applicable for this feature.
