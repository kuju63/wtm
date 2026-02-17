# Feature Specification: Release Binary Hash Files

**Feature Branch**: `006-release-hash-files`
**Created**: 2026-02-14
**Status**: Draft
**Input**: User description: "リリースファイル毎の整合性チェックを可能とするためにすべてのリリースバイナリはハッシュファイルを必要とするようにする。"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Automated Hash Generation on Release (Priority: P1)

As a release manager, when I create a new release, the system automatically generates hash files for all binary artifacts so that users can verify file integrity without manual intervention.

**Why this priority**: This is the foundation of the integrity verification feature. Without automatic hash generation, the feature cannot exist. This ensures every release has verifiable checksums from day one.

**Independent Test**: Can be fully tested by creating a release and verifying that hash files are generated for each binary artifact in the release assets.

**Acceptance Scenarios**:

1. **Given** a release workflow is triggered, **When** binary artifacts are built and published, **Then** a corresponding SHA256 hash file is generated for each binary file
2. **Given** multiple binary artifacts (e.g., Windows, Linux, macOS builds), **When** the release is published, **Then** each artifact has its own hash file with naming pattern `<artifact-name>.sha256`
3. **Given** a release is created, **When** hash generation completes, **Then** all hash files are included in the release assets alongside their corresponding binaries

---

### User Story 2 - User-Side Integrity Verification (Priority: P2)

As an end user who downloads release binaries, I can verify the integrity of downloaded files using provided hash files to ensure the files have not been corrupted or tampered with during download.

**Why this priority**: This provides the actual value to end users - the ability to verify their downloads. It depends on P1 (hash generation) but is independently testable once hash files exist.

**Independent Test**: Can be tested by downloading a binary and its hash file, then running a hash verification command to confirm the files match.

**Acceptance Scenarios**:

1. **Given** a user downloads a binary and its corresponding `.sha256` file, **When** the user runs a hash verification tool (e.g., `sha256sum -c <file>.sha256`), **Then** the verification succeeds if the binary is intact
2. **Given** a binary file is corrupted or modified after download, **When** the user verifies the hash, **Then** the verification fails and indicates a mismatch
3. **Given** a user wants to verify a download, **When** they access the release page, **Then** hash files are clearly visible in the release assets list

---

### User Story 3 - Hash Documentation in Release Notes (Priority: P3)

As a security-conscious user, I can view hash values directly in the release notes or documentation to quickly verify downloads without needing to download separate hash files.

**Why this priority**: This is a convenience enhancement that improves user experience but is not strictly necessary. Users can still verify integrity using hash files from P1 and P2.

**Independent Test**: Can be tested by viewing release notes and confirming that hash values are displayed in a readable format (e.g., table or code block).

**Acceptance Scenarios**:

1. **Given** a release is published, **When** a user views the release notes, **Then** a table or section lists all artifacts with their SHA256 hash values
2. **Given** hash values are displayed in release notes, **When** a user copies a hash value, **Then** the format is clean and ready for command-line verification tools (no extra whitespace or formatting characters)

---

### Edge Cases

- What happens when hash file generation fails during release creation?
  - System should fail the release workflow and notify the release manager
  - No release should be published without complete hash coverage
- How does the system handle binary artifacts that are added manually after initial release?
  - If binaries are added post-release, hash files must be generated and added manually or via re-triggering automation
- What happens if a user downloads a hash file but not the corresponding binary, or vice versa?
  - Release assets should clearly pair binaries and hash files (e.g., via naming convention or grouping)
- How does the system handle hash file naming conflicts?
  - Use consistent, predictable naming: `<binary-name>.<extension>.sha256` (e.g., `wt-win-x64.exe.sha256`)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST generate a SHA256 hash file for every binary artifact during the release process
- **FR-002**: Hash files MUST use the naming pattern `<artifact-name>.sha256` to maintain clear association with source binaries
- **FR-003**: Hash files MUST contain the SHA256 checksum followed by the filename in the format expected by standard verification tools (e.g., `sha256sum`)
- **FR-004**: System MUST include all generated hash files as release assets alongside their corresponding binaries
- **FR-005**: System MUST fail the release workflow if hash generation fails for any artifact, preventing incomplete releases
- **FR-006**: Hash values MUST be displayed in release notes in a user-friendly format (e.g., markdown table or code block)
- **FR-007**: System MUST support generation of hash files for all binary formats (executables, archives, installers)
- **FR-008**: Hash file content MUST be compatible with standard verification tools (`sha256sum`, `shasum`, PowerShell `Get-FileHash`)

### Key Entities

- **Binary Artifact**: A compiled executable or archive file included in a release (e.g., `wt-win-x64.exe`, `wt-linux-x64.tar.gz`)
  - Attributes: filename, size, platform target, file format
  - Relationship: Each binary has exactly one corresponding hash file
- **Hash File**: A text file containing the SHA256 checksum of a binary artifact
  - Attributes: filename (derived from binary), hash value (64-character hexadecimal string), algorithm (SHA256)
  - Format: `<hash> <filename>` (standard SHA256 checksum file format)
  - Relationship: Each hash file corresponds to exactly one binary artifact

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of binary artifacts in every release have corresponding SHA256 hash files available in release assets
- **SC-002**: Users can successfully verify binary integrity using standard verification methods without additional processing or formatting
- **SC-003**: Release workflow fails automatically if hash generation fails for any artifact, ensuring zero releases are published without complete hash coverage
- **SC-004**: Hash values are visible in release notes within 5 minutes of release publication, allowing users to verify downloads immediately
- **SC-005**: No support requests related to "how to verify download integrity" after hash files are implemented (establishes clear, self-service verification method)

## Assumptions

- **Hash Algorithm**: SHA256 is chosen as the industry-standard algorithm offering strong security while maintaining broad tool support
- **Hash File Format**: Standard SHA256 checksum format (`<hash> <filename>`) ensures compatibility with existing verification tools across all platforms
- **Release Platform**: Assumes releases are published via GitHub Releases or similar platform that supports multiple assets per release
- **Verification Responsibility**: Users are responsible for performing verification using their platform's native tools; the system only provides hash files and documentation
- **Automation Context**: Hash generation occurs during automated CI/CD release workflow (e.g., GitHub Actions) rather than manual processes
- **Artifact Scope**: Hash files are required for all binary distribution formats (executables, archives, installers) but not for source code archives (which have Git commit hashes)

## Dependencies

- CI/CD pipeline must support running hash generation commands (e.g., `sha256sum`, PowerShell `Get-FileHash`)
- Release workflow must support uploading multiple assets (binaries + hash files)
- Release notes generation process must support programmatic insertion of hash values (e.g., via template or script)

## Out of Scope

- Support for hash algorithms other than SHA256 (MD5, SHA1, SHA512) - can be added in future if needed
- Automatic verification on user's machine (this feature only provides hash files; users must verify manually)
- GPG/PGP signature files for cryptographic signing (separate security feature)
- Hash verification UI or desktop application (command-line verification only)
- Historical hash file generation for past releases (only applies to new releases going forward)
