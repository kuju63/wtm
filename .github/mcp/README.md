# MCP templates: Codacy

This repository uses **Codacy** for code quality/security analysis and uploads coverage in CI (see `.github/workflows/test.yml` and `.github/SECRETS.md`).

## What you need
- A Codacy token for API access in your client.
  - CI uses `CODACY_PROJECT_TOKEN` for coverage upload.
  - Your MCP client may require a personal Codacy API token instead; store it in your client’s secret store/environment.

## Values you’ll commonly need
- **Git provider / org / repo**: derive from `git remote -v` (e.g., `github.com/<org>/<repo>.git`).
- **Codacy project token** (for CI coverage upload): see `.github/SECRETS.md`.

## Suggested local env (template)
Copy `.github/mcp/codacy.env.example` into your local environment and fill in the token(s) required by your MCP client.

## Useful Codacy entry points in this repo
- `.codacy.yml`: analysis exclusions and enabled engines
- `.github/workflows/test.yml`: coverage report generation and Codacy upload
