# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Remove Worktree Command**: New `wt remove` command to safely delete git worktrees
  - Safe removal with validation (uncommitted changes, locked worktrees)
  - Force removal with `--force` flag to bypass uncommitted changes and locks
  - Protection against removing main worktree or current working directory
  - Human-readable and JSON output formats (`--output` flag)
  - Verbose error reporting with error codes (`--verbose` flag)
  - Actionable error messages with solutions (e.g., "Use 'wt list' to see available worktrees")
- **SBOM (Software Bill of Materials) Generation**: Complete supply chain transparency for all releases
  - SPDX 2.3 format (ISO/IEC 5962:2021 compliant)
  - Automatic generation with Microsoft SBOM Tool
  - Includes all direct and transitive dependencies
  - Multi-platform dependency resolution (Windows, Linux, macOS)
  - SPDX format validation with @spdx/spdx-validator
  - Required package verification (System.CommandLine, System.IO.Abstractions)
  - Package count threshold validation
  - SPDX 2.3+ compliance verification
- **GitHub Dependency Graph Integration**: Automatic submission via Dependency Submission API
  - Enables Dependabot vulnerability alerts
  - Supports Renovate automated dependency updates
  - Provides visibility in repository Insights → Dependency graph
- **SBOM Release Assets**: Downloadable SBOM files attached to every GitHub release
  - Filename format: `wt-{version}-sbom.spdx.json`
  - Signed with GPG for authenticity
  - Available for all releases
- **PR SBOM Testing Workflow**: Pre-release validation for pull requests
  - Dry-run SBOM generation on PR creation/update
  - SPDX format validation
  - Required package verification
  - Performance benchmarking (15-minute timeout)
  - PR comment with test results
  - Artifact upload for manual review
- **Documentation**: Comprehensive SBOM usage guides
  - ADR 004: SBOM Generation architectural decision record
  - SBOM Usage Guide with user and developer instructions
  - Example SBOM file for reference
  - README section explaining supply chain transparency
- **Performance Optimizations**:
  - NuGet package caching for faster builds
  - Performance metrics logging (cache hit/miss, duration tracking)
  - 15-minute workflow timeout (down from 25 minutes)

### Changed

- **Release Workflow**: Enhanced with complete SBOM generation pipeline
  - Replaced Anchore SBOM Action with Microsoft SBOM Tool
  - Changed SBOM format from CycloneDX to SPDX 2.3
  - Changed filename from `wt-*-sbom.json` to `wt-*-sbom.spdx.json`
  - Added multi-platform dependency restore
  - Added comprehensive validation steps
  - Added GitHub Dependency Graph submission

### Security

- **Supply Chain Security**: Full transparency of software components
  - Automatic vulnerability tracking via Dependabot
  - License compliance verification
  - Complete audit trail for all dependencies

## Notes

This release implements the complete SBOM generation feature as specified in:

- Specification: `specs/004-complete-sbom-generation/spec.md`
- Research: `specs/004-complete-sbom-generation/research.md`
- Implementation Plan: `specs/004-complete-sbom-generation/plan.md`
- ADR: `docs/adr/004-sbom-generation.md`

For more information, see the [SBOM Usage Guide](docs/guides/sbom-usage.md).
