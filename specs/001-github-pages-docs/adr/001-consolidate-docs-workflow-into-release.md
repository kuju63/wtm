# ADR 001: Consolidate Documentation Workflow into Release Pipeline

**Status**: Accepted  
**Date**: 2026-01-16  
**Deciders**: Development Team  
**Related Issues**: Bug fix for documentation not deploying on release

## Context

The original design specified a separate `.github/workflows/docs.yml` workflow triggered by `release.published` events. However, this approach encountered a fundamental GitHub Actions limitation:

**GitHub Actions Security Constraint**: When a workflow uses `GITHUB_TOKEN` to create a release (via `softprops/action-gh-release`), the release creation does NOT trigger other workflows to prevent recursive workflow execution.

**Impact**: Documentation would never be built and deployed automatically after release creation, defeating the purpose of automated documentation publishing (FR-001).

## Decision

We decided to **consolidate the documentation build and deployment into the release workflow** (`.github/workflows/release.yml`) as a dependent job named `build-and-deploy-docs`.

### Implementation

```yaml
# In .github/workflows/release.yml
build-and-deploy-docs:
  name: Build and Deploy Documentation
  needs: [calculate-version, create-release]
  runs-on: ubuntu-latest
  steps:
    # ... documentation build steps
```

**Key Changes**:

1. Removed separate `.github/workflows/docs.yml` workflow
2. Added `build-and-deploy-docs` job to `release.yml`
3. Job depends on `create-release` job completion
4. Version obtained from `needs.calculate-version.outputs.version` instead of `github.event.release.tag_name`
5. Added caching for NuGet and DocFX to improve performance
6. Updated actions to latest versions (checkout@v6, setup-dotnet@v5, cache@v5, gh-pages@v4)

## Alternatives Considered

### 1. Use Personal Access Token (PAT) instead of GITHUB_TOKEN

**Pros**:

- Would allow separate workflow trigger
- Maintains original design separation

**Cons**:

- Requires PAT with write permissions (security concern)
- PAT management overhead (rotation, expiration)
- Violates principle of least privilege
- Not suitable for open source projects (requires organization/user PAT)

**Rejected**: Security and maintenance concerns outweigh architectural purity.

### 2. Use workflow_run trigger

```yaml
on:
  workflow_run:
    workflows: ["Release to GitHub"]
    types: [completed]
```

**Pros**:

- Maintains workflow separation
- Uses GITHUB_TOKEN

**Cons**:

- Introduces delay (wait for release workflow completion)
- More complex error handling (need to check workflow_run conclusion)
- Harder to debug (two separate workflow runs)
- Version information harder to pass between workflows

**Rejected**: Added complexity without clear benefit.

### 3. Use repository_dispatch custom event

**Pros**:

- Maximum flexibility
- Can pass custom payloads

**Cons**:

- Requires additional API calls in release workflow
- More code to maintain
- Overkill for simple use case

**Rejected**: Unnecessary complexity.

## Consequences

### Positive

1. **Works correctly**: Documentation automatically deploys after release
2. **Simpler architecture**: Single workflow, easier to understand and debug
3. **Better performance**: Shared caching between release and docs jobs
4. **Consistent versioning**: Version calculated once, used by both release and docs
5. **Atomic operation**: Release and docs deployment happen in same workflow run
6. **No secrets required**: Uses GITHUB_TOKEN throughout

### Negative

1. **Tighter coupling**: Documentation deployment is now part of release workflow
2. **Cannot manually trigger docs rebuild**: Would need to trigger entire release workflow or add separate docs-only workflow for maintenance
3. **Longer workflow time**: Release workflow now includes documentation build time (adds ~3-5 minutes)
4. **All-or-nothing**: If docs build fails, entire release workflow fails (can be mitigated with `continue-on-error`)

### Mitigation Strategies

For the negative consequences:

1. **Manual docs rebuild**: Can add a separate `docs-manual.yml` workflow with `workflow_dispatch` trigger for emergency documentation updates without creating a release
2. **Workflow time**: Mitigated through caching (NuGet, DocFX)
3. **Build failures**: Documentation build is final step, so release artifacts are already created and published before docs build

## Compliance

This decision aligns with project constitution principles:

- **III. Clean & Secure Code**: Reduces complexity by consolidating workflows
- **V. Minimal Dependencies**: No additional dependencies or secrets required
- **VI. Evidence-Based Development**: Based on empirical testing of GitHub Actions behavior

## Notes

- This ADR should be referenced in specification contracts to explain the architectural decision
- Future work: Consider adding separate `docs-manual.yml` for emergency updates
- Monitor workflow execution time; if documentation build becomes bottleneck, revisit separation strategy with PAT approach

## References

- GitHub Actions Documentation: [Triggering a workflow from a workflow](https://docs.github.com/en/actions/using-workflows/triggering-a-workflow#triggering-a-workflow-from-a-workflow)
- Original spec: `specs/001-github-pages-docs/spec.md`
- Implementation: `.github/workflows/release.yml`
- Bug report: Documentation not deploying on release
