# DocFX Configuration Contract

**File**: `docfx.json`  
**Purpose**: Defines the contract for DocFX configuration to generate documentation from .NET projects and markdown files.

## Schema Reference

```json
{
  "$schema": "https://raw.githubusercontent.com/dotnet/docfx/main/schemas/docfx.schema.json"
}
```

**Contract**: MUST reference official DocFX v2 schema for validation.

## Metadata Section

### Purpose

Extracts API documentation from .NET projects using Roslyn.

### Contract

```json
{
  "metadata": [
    {
      "src": [
        {
          "src": "./wt.cli",
          "files": ["**/*.csproj"]
        }
      ],
      "output": "api",
      "shouldSkipMarkdeep": false,
      "properties": {
        "TargetFramework": "net10.0"
      }
    }
  ]
}
```

**Requirements**:

| Field | Type | Required | Validation | Purpose |
|-------|------|----------|------------|---------|
| `src[].src` | string | Yes | MUST be valid directory path | Source project directory |
| `src[].files` | array | Yes | MUST match at least one `.csproj` file | Project files to process |
| `output` | string | Yes | MUST be `api` | Output directory for generated API docs |
| `shouldSkipMarkdeep` | boolean | No | Default: false | Enable Markdeep rendering |
| `properties.TargetFramework` | string | Yes | MUST match project's `<TargetFramework>` | Framework version for API extraction |

**Validation Rules**:

- `src` directory MUST exist relative to docfx.json
- At least one `.csproj` file MUST be found
- `.csproj` MUST have `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- Build MUST succeed in Release configuration
- API extraction MUST NOT fail on missing XML docs (warnings OK if not using `--warningsAsErrors`)

## Build Section

### Content Subsection

**Purpose**: Defines documentation source files to include.

**Contract**:

```json
{
  "build": {
    "content": [
      {
        "files": ["**/*.{md,yml}"],
        "exclude": [
          "_site/**",
          "obj/**",
          "bin/**",
          "specs/**",
          ".specify/**",
          "Tools/**",
          "coverage/**"
        ]
      }
    ]
  }
}
```

**Requirements**:

| Field | Type | Required | Validation | Purpose |
|-------|------|----------|------------|---------|
| `files` | array | Yes | MUST include markdown and/or YAML | Content file patterns |
| `exclude` | array | Yes | MUST exclude build artifacts | Directories to ignore |

**Include Patterns**:

- `**/*.md` - All markdown files (recursive)
- `**/*.yml` - YAML TOC and metadata files

**Exclude Patterns (REQUIRED)**:

- `_site/**` - DocFX output directory
- `obj/**`, `bin/**` - .NET build artifacts
- `specs/**` - Feature specifications (not user docs)
- `.specify/**` - Spec Kit internal files
- `Tools/**` - Build tools source code
- `coverage/**` - Test coverage reports

**Validation Rules**:

- At least one markdown file MUST be found
- Excluded directories MUST NOT be scanned
- Invalid markdown syntax MUST cause build failure with `--warningsAsErrors`

### Resource Subsection

**Purpose**: Defines static assets (images, CSS, JS) to copy.

**Contract**:

```json
{
  "build": {
    "resource": [
      {
        "files": ["images/**", "assets/**", "*.png", "*.jpg"]
      }
    ]
  }
}
```

**Requirements**:

| Field | Type | Required | Validation | Purpose |
|-------|------|----------|------------|---------|
| `files` | array | Yes | Valid glob patterns | Asset file patterns |

**Include Patterns**:

- `images/**` - Image directory (if exists)
- `assets/**` - General assets directory (if exists)
- `*.png`, `*.jpg` - Root-level images

**Validation Rules**:

- Resources MUST be copied to output without modification
- Missing resources MUST NOT cause build failure (warning only)
- Binary files MUST be copied as-is

### Output and Template

**Purpose**: Defines output directory and DocFX themes.

**Contract**:

```json
{
  "build": {
    "output": "_site",
    "template": ["default", "modern"]
  }
}
```

**Requirements**:

| Field | Type | Required | Validation | Purpose |
|-------|------|----------|------------|---------|
| `output` | string | Yes | MUST be valid directory name | Output directory for generated site |
| `template` | array | Yes | MUST include at least one valid template | DocFX themes to apply |

**Template Options**:

- `default` - Classic DocFX template
- `modern` - Modern, responsive template
- Custom templates can be added to `templates/` directory

**Validation Rules**:

- Output directory MUST be writable
- Templates MUST exist in DocFX installation or local `templates/` directory
- Multiple templates are merged (modern overrides default)

### Global Metadata

**Purpose**: Site-wide metadata injected into all pages.

**Contract**:

```json
{
  "build": {
    "globalMetadata": {
      "_appName": "wt",
      "_appTitle": "wt - Git Worktree Manager",
      "_appFooter": "Copyright © 2026 Kuju63. Licensed under MIT.",
      "_enableSearch": true,
      "_enableNewTab": true,
      "_gitHub": {
        "repo": "kuju63/wt",
        "branch": "main"
      },
      "_gitContribute": {
        "repo": "kuju63/wt",
        "branch": "main",
        "path": "docs"
      }
    }
  }
}
```

**Requirements**:

| Field | Type | Required | Validation | Purpose |
|-------|------|----------|------------|---------|
| `_appName` | string | Yes | Non-empty | Short application name |
| `_appTitle` | string | Yes | Non-empty | Full application title for `<title>` |
| `_appFooter` | string | No | - | Footer text on all pages |
| `_enableSearch` | boolean | Yes | MUST be true | Enable client-side search |
| `_enableNewTab` | boolean | No | Default: false | Open external links in new tab |
| `_gitHub.repo` | string | Yes | Format: `owner/repo` | GitHub repository |
| `_gitHub.branch` | string | Yes | Valid branch name | Default branch for links |
| `_gitContribute.repo` | string | Yes | Format: `owner/repo` | Same as `_gitHub.repo` |
| `_gitContribute.branch` | string | Yes | Valid branch name | Branch for "Edit this page" links |
| `_gitContribute.path` | string | Yes | Valid directory path | Documentation source directory |

**Validation Rules**:

- All required fields MUST be present
- GitHub repo links MUST be valid format
- "Edit this page" links MUST point to correct documentation source

### Sitemap Configuration

**Purpose**: SEO optimization via XML sitemap generation.

**Contract**:

```json
{
  "build": {
    "sitemap": {
      "baseUrl": "https://kuju63.github.io/wt/",
      "priority": 1.0,
      "changefreq": "weekly"
    }
  }
}
```

**Requirements**:

| Field | Type | Required | Validation | Purpose |
|-------|------|----------|------------|---------|
| `baseUrl` | string | Yes | MUST be valid HTTPS URL with trailing `/` | Site base URL for sitemap |
| `priority` | number | No | Range: 0.0 - 1.0 | Page priority for search engines |
| `changefreq` | string | No | Valid values: always, hourly, daily, weekly, monthly, yearly, never | Expected update frequency |

**Validation Rules**:

- `baseUrl` MUST end with `/`
- `baseUrl` MUST use HTTPS protocol
- Sitemap MUST be generated at `_site/sitemap.xml`

### Cross-Reference Configuration

**Purpose**: Enable linking to external API documentation.

**Contract**:

```json
{
  "build": {
    "xref": [
      "https://learn.microsoft.com/en-us/dotnet/api"
    ]
  }
}
```

**Requirements**:

| Field | Type | Required | Validation | Purpose |
|-------|------|----------|------------|---------|
| `xref` | array | No | Valid URLs | External API reference URLs |

**Validation Rules**:

- URLs MUST be accessible during build
- Cross-references to .NET types (e.g., `System.String`) automatically link to Microsoft Docs
- Failed xref lookups produce warnings (not errors)

## Complete Example Configuration

```json
{
  "$schema": "https://raw.githubusercontent.com/dotnet/docfx/main/schemas/docfx.schema.json",
  "metadata": [
    {
      "src": [
        {
          "src": "./wt.cli",
          "files": ["**/*.csproj"]
        }
      ],
      "output": "api",
      "shouldSkipMarkdeep": false,
      "properties": {
        "TargetFramework": "net10.0"
      }
    }
  ],
  "build": {
    "content": [
      {
        "files": ["**/*.{md,yml}"],
        "exclude": [
          "_site/**",
          "obj/**",
          "bin/**",
          "specs/**",
          ".specify/**",
          "Tools/**",
          "coverage/**"
        ]
      }
    ],
    "resource": [
      {
        "files": ["images/**", "assets/**", "*.png", "*.jpg"]
      }
    ],
    "output": "_site",
    "template": ["default", "modern"],
    "globalMetadata": {
      "_appName": "wt",
      "_appTitle": "wt - Git Worktree Manager",
      "_appFooter": "Copyright © 2026 Kuju63. Licensed under MIT.",
      "_enableSearch": true,
      "_enableNewTab": true,
      "_gitHub": {
        "repo": "kuju63/wt",
        "branch": "main"
      },
      "_gitContribute": {
        "repo": "kuju63/wt",
        "branch": "main",
        "path": "docs"
      }
    },
    "sitemap": {
      "baseUrl": "https://kuju63.github.io/wt/",
      "priority": 1.0,
      "changefreq": "weekly"
    },
    "xref": [
      "https://learn.microsoft.com/en-us/dotnet/api"
    ]
  }
}
```

## Build Command Contract

### Standard Build

```bash
docfx build docfx.json
```

**Output**:

- Generates site in `_site/` directory
- Warnings are logged but don't fail build

### Strict Build (Required for CI/CD)

```bash
docfx build docfx.json --warningsAsErrors
```

**Output**:

- Warnings cause build failure (exit code 1)
- Enforces documentation quality

### Versioned Build (For Deployment)

```bash
docfx build docfx.json --warningsAsErrors -o _output/v1.2
```

**Output**:

- Generates site in custom output directory
- Used for version-specific documentation

## Validation Checklist

### Pre-Build Validation

- ✅ `docfx.json` is valid JSON
- ✅ Schema validation passes
- ✅ `src` directory exists
- ✅ At least one `.csproj` found
- ✅ `.csproj` has XML documentation enabled
- ✅ All required metadata fields present

### Build Validation

- ✅ .NET project builds successfully
- ✅ XML documentation generated
- ✅ Markdown files found
- ✅ No broken internal links
- ✅ TOC files (`toc.yml`) valid
- ✅ Templates applied successfully

### Post-Build Validation

- ✅ `_site/` directory created
- ✅ `_site/index.html` exists
- ✅ `_site/api/` directory exists (if API docs enabled)
- ✅ `_site/sitemap.xml` exists
- ✅ Search index (`index.json`) generated

## Common Issues and Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| "Could not find file" error | Invalid `src` path | Verify path relative to `docfx.json` |
| No API documentation generated | XML docs not enabled | Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to `.csproj` |
| Broken link warnings | Invalid markdown links | Check `[text](path)` syntax and file existence |
| Template not found | Custom template missing | Ensure template exists in `templates/` or use built-in |
| Cross-reference failures | External xref URL down | Check network connectivity, verify URL |

## Version Compatibility

| DocFX Version | .NET Version | Status | Notes |
|---------------|--------------|--------|-------|
| 2.78.4 | .NET 10.0 | ✅ Supported | Current version with Roslyn 4.13.0 |
| 2.77.x | .NET 10.0 | ⚠️ Partial | May lack .NET 10 features |
| 2.70.x | .NET 10.0 | ❌ Not supported | Use 2.78.4+ |

**Contract**: MUST use DocFX 2.78.4 or later for .NET 10 support.

---

**Next**: `quickstart.md` - Quick start guide for implementing the feature
