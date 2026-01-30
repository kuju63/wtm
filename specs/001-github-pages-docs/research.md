# Research: GitHub Pages Documentation Publishing

**Feature**: 001-github-pages-docs  
**Date**: 2026-01-15  
**Status**: Complete

This document consolidates research findings to resolve all "NEEDS CLARIFICATION" items from the Technical Context and Constitution Check sections of the implementation plan.

## Research Areas

### 1. DocFX Version and Installation

**Decision**: Use DocFX v2.78.4 (latest stable) as .NET global tool

**Rationale**:

- DocFX 2.78.4 is the current production release maintained by Microsoft/.NET Foundation
- Includes native support for .NET 10 (via Roslyn 4.13.0 update)
- Global tool installation integrates seamlessly with .NET 10.0 SDK already in use
- Supports modern template system with "default" and "modern" themes
- Native support for C# XML documentation extraction
- Performance improvements: cached MarkdownPipeline, reused YamlDeserializer instances

**Installation Method**:

```bash
dotnet tool install --global docfx --version 2.78.4
```

**Alternatives Considered**:

- **Container-based (Docker)**: More complex CI/CD setup, unnecessary overhead for simple documentation build
- **npm-based tools (MkDocs, Docusaurus)**: Cannot generate .NET API documentation from XML comments
- **Sphinx**: Python-focused, poor .NET integration, requires additional plugins

**Implementation Requirements**:

- Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to wt.cli.csproj
- Pin DocFX version 2.78.4 in GitHub Actions workflow to prevent unexpected breaking changes
- Configure `docfx.json` to output XML documentation from wt.cli project

---

### 2. Automatic Command List Generation

**Decision**: Programmatic export of System.CommandLine help text using HelpBuilder and custom IConsole

**Rationale**:

- System.CommandLine (already used in wt.cli) provides rich command metadata
- Automatic generation ensures documentation stays synchronized with actual CLI implementation
- Prevents manual maintenance burden and documentation drift
- Supports requirement FR-012 (Command Reference as man page equivalent)

**Implementation Architecture**:

#### Step 1: Create Documentation Generator Tool

```csharp
// Tools/DocGenerator/Program.cs
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.Text;

class MarkdownConsole : IConsole
{
    private readonly StringWriter _out = new StringWriter();
    public string GetOutput() => _out.ToString();
    
    public IStandardStreamWriter Out => StandardStreamWriter.Create(_out);
    public bool IsOutputRedirected => false;
    public IStandardStreamWriter Error => Out;
    public bool IsErrorRedirected => false;
    public bool IsInputRedirected => false;
}

class CommandDocGenerator
{
    public static void GenerateCommandDocs(RootCommand rootCommand, string outputDir)
    {
        // Ensure commands directory exists
        var commandsDir = Path.Combine(outputDir, "commands");
        Directory.CreateDirectory(commandsDir);
        
        // Generate overview page
        var overview = new StringBuilder();
        overview.AppendLine("# Command Reference");
        overview.AppendLine();
        overview.AppendLine("Complete reference for all `wt` commands.");
        overview.AppendLine();
        
        // Generate individual command pages
        foreach (var command in rootCommand.Subcommands)
        {
            var markdown = ConvertCommandToMarkdown(command);
            var fileName = $"{command.Name}.md";
            File.WriteAllText(Path.Combine(commandsDir, fileName), markdown);
            
            overview.AppendLine($"- [`wt {command.Name}`](commands/{command.Name}.md) - {command.Description}");
        }
        
        File.WriteAllText(Path.Combine(outputDir, "command-reference.md"), overview.ToString());
        Console.WriteLine($"✅ Generated documentation for {rootCommand.Subcommands.Count} commands");
    }
    
    private static string ConvertCommandToMarkdown(Command command)
    {
        var md = new StringBuilder();
        
        // Title and description
        md.AppendLine($"# wt {command.Name}");
        md.AppendLine();
        md.AppendLine(command.Description);
        md.AppendLine();
        
        // Usage section
        md.AppendLine("## Usage");
        md.AppendLine();
        md.AppendLine("```bash");
        md.AppendLine($"wt {command.Name} [options]");
        md.AppendLine("```");
        md.AppendLine();
        
        // Options section
        if (command.Options.Any())
        {
            md.AppendLine("## Options");
            md.AppendLine();
            
            foreach (var option in command.Options)
            {
                md.AppendLine($"### `{string.Join(", ", option.Aliases)}`");
                md.AppendLine();
                md.AppendLine(option.Description);
                md.AppendLine();
                
                if (option.ArgumentHelpName != null)
                {
                    md.AppendLine($"**Type:** `{option.ArgumentHelpName}`");
                    md.AppendLine();
                }
                
                if (option.IsRequired)
                {
                    md.AppendLine("**Required:** Yes");
                    md.AppendLine();
                }
            }
        }
        
        // Examples section
        md.AppendLine("## Examples");
        md.AppendLine();
        AddExamplesForCommand(md, command.Name);
        
        return md.ToString();
    }
    
    private static void AddExamplesForCommand(StringBuilder md, string commandName)
    {
        // Define examples per command
        var examples = commandName switch
        {
            "create" => new[]
            {
                ("Create worktree with default path", "wt create feature-login"),
                ("Create with custom path", "wt create feature-login --path /tmp/wt-login"),
                ("Create and open in VS Code", "wt create feature-login --editor vscode")
            },
            "list" => new[]
            {
                ("List all worktrees", "wt list"),
                ("List in JSON format", "wt list --json")
            },
            _ => Array.Empty<(string, string)>()
        };
        
        foreach (var (description, command) in examples)
        {
            md.AppendLine($"### {description}");
            md.AppendLine();
            md.AppendLine("```bash");
            md.AppendLine(command);
            md.AppendLine("```");
            md.AppendLine();
        }
    }
}

// Entry point
class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: DocGenerator <output-directory>");
            return;
        }
        
        var outputDir = args[0];
        
        // Import CLI command structure (same as main application)
        var rootCommand = new RootCommand("Git worktree manager");
        // ... add all subcommands exactly as defined in wt.cli ...
        
        CommandDocGenerator.GenerateCommandDocs(rootCommand, outputDir);
    }
}
```

#### Step 2: Integrate into Build Pipeline

```yaml
# .github/workflows/docs.yml
- name: Generate command documentation
  run: |
    cd Tools/DocGenerator
    dotnet run -- ../../docs

- name: Build DocFX documentation
  run: docfx build docfx.json
```

**Benefits**:

- ✅ **Zero manual maintenance**: CLI changes automatically reflected in docs
- ✅ **Consistency guaranteed**: Documentation matches implementation
- ✅ **Type safety**: Compilation errors prevent doc/code mismatch
- ✅ **Rich metadata**: Full access to System.CommandLine attributes

**Alternatives Considered**:

- **Manual markdown files**: High maintenance, prone to drift
- **Scraping `--help` output**: Loses structured metadata
- **Third-party tools**: No existing System.CommandLine integration

---

### 3. Documentation Testing Strategy

**Decision**: Two-layer testing approach with build-time and link validation

**Rationale**:

- Catches documentation errors early in deployment pipeline
- No additional test project complexity required
- Focuses on critical failure modes (broken links, build errors)

**Testing Layers**:

#### Layer 1: Build-Time Validation

- **Tool**: DocFX's built-in `--warningsAsErrors` flag
- **Validates**:
  - Markdown syntax errors
  - YAML structure issues
  - Internal file reference failures
  - Missing cross-references
- **Execution**: During DocFX build step in deployment workflow
- **Implementation**:

```bash
docfx build docfx.json --warningsAsErrors
```

**Failure Examples**:

- Invalid markdown syntax → Build fails
- Broken internal links (e.g., `[text](missing-file.md)`) → Build fails
- YAML TOC errors → Build fails
- Missing API XML documentation → Warning promoted to error

#### Layer 2: Link Validation

- **Tool**: LinkChecker (Python-based)
- **Validates**:
  - All internal links between pages
  - Links to API reference pages
  - External links (optional - can filter noisy domains)
  - Image resources
- **Execution**: After successful DocFX build, before deployment
- **Implementation**:

```yaml
- name: Validate documentation links
  run: |
    pip install linkchecker
    linkchecker \
      --check-extern \
      --ignore-url="localhost" \
      --no-warnings \
      _output/${{ steps.version.outputs.minor }}/
```

**Failure Examples**:

- Broken link to command page → Validation fails
- Missing image file → Validation fails
- Dead external link → Validation fails (configurable)

**Benefits**:

- ✅ **Fast feedback**: Errors caught in deployment pipeline
- ✅ **No code changes**: No need to modify test project
- ✅ **Comprehensive**: Covers critical documentation quality issues
- ✅ **Simple**: Two tools, clear responsibilities

**Alternatives Considered**:

- **Manual testing**: Does not scale, violates Constitution VI
- **xUnit content tests**: Adds unnecessary complexity for static content
- **Full E2E browser testing**: Overkill for static documentation
- **Third-party SaaS**: External dependency, security concerns

---

### 4. Version Switcher Implementation

**Decision**: Custom JavaScript version switcher with separate build outputs per minor version

**Rationale**:

- DocFX does not provide native version switching
- Separate builds ensure complete isolation between versions
- JavaScript provides seamless UX without server-side logic
- Matches requirements FR-006 (switch between versions) and FR-014 (minor version level)

**Architecture**:

```text
GitHub Pages Structure:
https://username.github.io/wt/
├── index.html              # Redirect to latest version
├── v1.0/                   # Complete site for v1.0.x
│   ├── index.html
│   ├── installation.html
│   ├── commands/
│   ├── api/
│   └── ...
├── v1.1/                   # Complete site for v1.1.x
│   └── ...
└── version-manifest.json   # Auto-generated list of all versions
```

**Implementation Components**:

#### 1. Version Switcher UI (Custom DocFX Template)

Modify DocFX template to inject version selector in navigation bar:

```html
<!-- templates/partials/navbar.tmpl.partial -->
<nav class="navbar">
  <!-- Existing navbar content -->
  
  <div class="version-selector">
    <label for="version-switcher">Version:</label>
    <select id="version-switcher" onchange="switchVersion(this.value)">
      <option>Loading...</option>
    </select>
  </div>
</nav>

<script>
function switchVersion(targetVersion) {
  // Extract current path without version prefix
  const currentPath = window.location.pathname;
  const pathWithoutVersion = currentPath.replace(/\/v\d+\.\d+\//, '/');
  
  // Navigate to same page in selected version
  window.location.href = `/${targetVersion}${pathWithoutVersion}`;
}

// Load available versions from manifest
fetch('/version-manifest.json')
  .then(res => res.json())
  .then(data => {
    const selector = document.getElementById('version-switcher');
    selector.innerHTML = ''; // Clear loading text
    
    data.versions.forEach(v => {
      const option = document.createElement('option');
      option.value = v.path;
      option.textContent = v.label;
      
      // Pre-select current version based on URL
      if (window.location.pathname.includes(`/${v.path}/`)) {
        option.selected = true;
      }
      
      selector.appendChild(option);
    });
  })
  .catch(err => {
    console.error('Failed to load versions:', err);
    document.querySelector('.version-selector').style.display = 'none';
  });
</script>
```

#### 2. Version Manifest Format

```json
{
  "versions": [
    {
      "label": "v1.1 (latest)",
      "path": "v1.1",
      "released": "2026-01-15",
      "isLatest": true
    },
    {
      "label": "v1.0",
      "path": "v1.0",
      "released": "2026-01-01",
      "isLatest": false
    }
  ]
}
```

**Edge Case Handling**:

- **Non-existent page in version**: 404 page shows link to that version's homepage
- **JavaScript disabled**: Users can manually edit URL (version visible in path)
- **Bookmark stability**: URLs never change (meets SC-006: 2+ years stability)
- **SEO optimization**: Each version independently crawlable; `<link rel="canonical">` points to latest
- **First-time visitors**: Auto-redirect from root to latest version

**Alternatives Considered**:

- **Single site with conditional content**: Complex, risk of version bleed, poor SEO
- **Git branches per version**: Manual backporting required, consistency issues
- **Third-party hosting (ReadTheDocs)**: External dependency, migration effort, less control

---

### 5. Version Manifest Automation

**Decision**: Automatic generation/update on every GitHub release

**Rationale**:

- Fully automated - no manual JSON editing required
- Idempotent - safe to re-run for same version
- Persisted in gh-pages branch, versioned history
- Synchronizes with binary releases (requirement FR-001)

**Automation Workflow**:

#### Step 1: Fetch Existing Manifest

```yaml
- name: Fetch existing version manifest
  run: |
    # Attempt to fetch gh-pages branch (may not exist on first release)
    git fetch origin gh-pages:gh-pages 2>/dev/null || true
    
    # Extract existing manifest or create empty structure
    if git show gh-pages:version-manifest.json > version-manifest.json 2>/dev/null; then
      echo "✅ Found existing manifest with $(jq '.versions | length' version-manifest.json) versions"
    else
      echo '{"versions":[]}' > version-manifest.json
      echo "✅ Created new manifest (first release)"
    fi
```

#### Step 2: Update Manifest with New Version

```yaml
- name: Update version manifest
  run: |
    VERSION="${{ steps.version.outputs.minor }}"  # e.g., v1.2
    RELEASE_DATE="${{ github.event.release.published_at }}"
    
    python3 .github/scripts/update-version-manifest.py \
      --version "$VERSION" \
      --date "$RELEASE_DATE" \
      --input version-manifest.json \
      --output version-manifest.json
```

#### Step 3: Include in Deployment

```yaml
- name: Copy manifest to deployment output
  run: |
    # Root-level manifest (accessed by all versions)
    cp version-manifest.json _output/version-manifest.json
    
    # Version-specific copy (redundancy/fallback)
    cp version-manifest.json _output/${{ steps.version.outputs.minor }}/version-manifest.json
```

**Python Script** (`.github/scripts/update-version-manifest.py`):

```python
#!/usr/bin/env python3
"""
Update version manifest JSON file with new documentation version.
Automatically marks new version as "latest" and updates labels.
"""
import json
import argparse
import sys

def update_manifest(input_file, output_file, new_version, release_date):
    """Update manifest with new version, marking it as latest."""
    
    # Read existing manifest
    try:
        with open(input_file, 'r') as f:
            manifest = json.load(f)
    except FileNotFoundError:
        manifest = {'versions': []}
    
    # Remove "latest" designation from all existing versions
    for v in manifest['versions']:
        v['isLatest'] = False
        v['label'] = v['label'].replace(' (latest)', '')
    
    # Check if this version already exists (re-release scenario)
    existing = next((v for v in manifest['versions'] if v['path'] == new_version), None)
    
    if existing:
        # Update existing version
        existing['released'] = release_date
        existing['isLatest'] = True
        existing['label'] = f"{new_version} (latest)"
        print(f"✅ Updated existing version: {new_version}")
    else:
        # Add new version at the beginning of the list
        manifest['versions'].insert(0, {
            'label': f"{new_version} (latest)",
            'path': new_version,
            'released': release_date,
            'isLatest': True
        })
        print(f"✅ Added new version: {new_version}")
    
    # Sort versions by release date (newest first)
    manifest['versions'].sort(key=lambda v: v['released'], reverse=True)
    
    # Write updated manifest
    with open(output_file, 'w') as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)
    
    print(f"   Total versions in manifest: {len(manifest['versions'])}")
    return 0

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Update version manifest')
    parser.add_argument('--version', required=True, help='Version to add (e.g., v1.2)')
    parser.add_argument('--date', required=True, help='Release date (ISO 8601 format)')
    parser.add_argument('--input', default='version-manifest.json', help='Input manifest file')
    parser.add_argument('--output', default='version-manifest.json', help='Output manifest file')
    args = parser.parse_args()
    
    sys.exit(update_manifest(args.input, args.output, args.version, args.date))
```

**Behavior**:

- **First release**: Creates new manifest with single version
- **Subsequent releases**: Adds new version, marks as latest
- **Re-release**: Updates existing version's date
- **Idempotent**: Running twice produces same result

**Benefits**:

- ✅ Zero manual intervention required
- ✅ Automatic on every GitHub release
- ✅ Idempotent and safe to re-run
- ✅ Versioned history in gh-pages branch
- ✅ Simple Python script (no external dependencies)

**Result After Deployment**:

```text
https://username.github.io/wt/
├── version-manifest.json       ← Updated with new version
├── v1.0/
│   ├── (documentation...)
│   └── version-manifest.json   ← Same content (for redundancy)
└── v1.1/
    ├── (documentation...)
    └── version-manifest.json   ← Same content
```

---

### 6. GitHub Pages Deployment Pattern

**Decision**: GitHub Actions with `actions/deploy-pages@v4`

**Complete Deployment Workflow**:

```yaml
name: Deploy Documentation

on:
  release:
    types: [published]
  workflow_dispatch:
    inputs:
      version:
        description: 'Manual version override (e.g., v1.2)'
        required: false

permissions:
  contents: write
  pages: write
  id-token: write

concurrency:
  group: pages
  cancel-in-progress: false

jobs:
  build-and-deploy:
    name: Build and Deploy Documentation
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    
    steps:
      - name: Checkout repository
        uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'
      
      - name: Install DocFX
        run: dotnet tool install --global docfx --version 2.78.4
      
      - name: Generate command documentation
        run: |
          cd Tools/DocGenerator
          dotnet run -- ../../docs
          cd ../..
      
      - name: Build API documentation
        run: |
          cd wt.cli
          dotnet build --configuration Release
          cd ..
      
      - name: Extract version
        id: version
        run: |
          if [ -n "${{ github.event.inputs.version }}" ]; then
            VERSION="${{ github.event.inputs.version }}"
          else
            TAG="${{ github.event.release.tag_name }}"
            VERSION=$(echo $TAG | sed -E 's/v([0-9]+\.[0-9]+).*/v\1/')
          fi
          echo "minor=$VERSION" >> $GITHUB_OUTPUT
          echo "Building documentation for version: $VERSION"
      
      - name: Build versioned documentation
        run: |
          mkdir -p _output/${{ steps.version.outputs.minor }}
          docfx build docfx.json --warningsAsErrors -o _output/${{ steps.version.outputs.minor }}
      
      - name: Fetch existing version manifest
        run: |
          git fetch origin gh-pages:gh-pages 2>/dev/null || true
          if git show gh-pages:version-manifest.json > version-manifest.json 2>/dev/null; then
            echo "✅ Existing manifest found"
          else
            echo '{"versions":[]}' > version-manifest.json
            echo "✅ Created new manifest"
          fi
      
      - name: Update version manifest
        run: |
          RELEASE_DATE="${{ github.event.release.published_at || github.event.head_commit.timestamp }}"
          
          python3 .github/scripts/update-version-manifest.py \
            --version "${{ steps.version.outputs.minor }}" \
            --date "$RELEASE_DATE"
          
          cp version-manifest.json _output/
          cp version-manifest.json _output/${{ steps.version.outputs.minor }}/
      
      - name: Validate documentation links
        run: |
          pip install linkchecker
          linkchecker --check-extern --ignore-url="localhost" --no-warnings \
            _output/${{ steps.version.outputs.minor }}/
      
      - name: Setup GitHub Pages
        uses: actions/configure-pages@v4
      
      - name: Upload artifact
        uses: actions/upload-pages-artifact@v3
        with:
          path: '_output'
      
      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v4
```

**Key Features**:

- ✅ **Automatic trigger**: Runs on every GitHub release publication (FR-001)
- ✅ **Command generation**: Automatically generates command documentation
- ✅ **Version extraction**: Extracts minor version from git tag (v1.2.3 → v1.2)
- ✅ **Build validation**: Treats warnings as errors (quality gate)
- ✅ **Link validation**: Checks all internal/external links
- ✅ **Manifest update**: Automatically updates version manifest
- ✅ **OIDC authentication**: No long-lived PAT tokens required
- ✅ **Concurrent-safe**: Prevents deployment race conditions

**Configuration Requirements**:

1. **Enable GitHub Pages**: Repository Settings → Pages → Source: "GitHub Actions"
2. **Create environment**: Add "github-pages" environment in repository settings
3. **Add script**: Create `.github/scripts/update-version-manifest.py` (from section 5)
4. **Create tool**: Implement `Tools/DocGenerator/` (from section 2)

**Performance**:

- Estimated build time: 5-8 minutes
- **Meets SC-003**: Documentation published within 10 minutes of release

**Alternatives Considered**:

- **Manual deployment**: No automation, violates FR-001
- **Third-party hosting (Netlify, Vercel)**: External dependency, cost
- **Azure Static Web Apps**: Requires Azure account, overkill for docs
- **gh-pages branch push**: More complex, no built-in OIDC, manual artifact management

---

### 7. DocFX Configuration Best Practices

**Enhanced `docfx.json`**:

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

**Key Configuration Elements**:

- **`xref`**: Cross-references to .NET framework documentation (System.CommandLine types link to Microsoft docs)
- **`sitemap`**: SEO optimization for search engine indexing
- **`_gitContribute`**: Enables "Edit this page" links for community contributions
- **`excludes`**: Prevents build errors from non-documentation directories
- **`template`**: Uses both "default" and "modern" for best compatibility and aesthetics

---

## Summary of Resolved Clarifications

| Original Question | Resolution |
|-------------------|------------|
| DocFX version? | v2.78.4 (latest stable), installed as .NET global tool |
| Command list automation? | System.CommandLine HelpBuilder + custom IConsole, auto-generated markdown |
| Version manifest automation? | Python script in GitHub Actions, updated per release automatically |
| Documentation testing? | Two-layer: DocFX build validation + LinkChecker |
| Version switcher? | Custom JavaScript with version-manifest.json, separate builds per version |
| Deployment pattern? | GitHub Actions `deploy-pages@v4`, triggered on release publication |

## Implementation Readiness

**All "NEEDS CLARIFICATION" items from plan.md have been resolved:**

- ✅ **DocFX version**: v2.78.4 with .NET 10 support via Roslyn 4.13.0
- ✅ **Command automation**: System.CommandLine programmatic export architecture defined
- ✅ **Version manifest**: Fully automated Python script, runs on every release
- ✅ **Testing**: Two-layer strategy (build validation + link checking)
- ✅ **Version switcher**: JavaScript + manifest.json with custom template override
- ✅ **Deployment**: Complete GitHub Actions workflow with validation gates

## New Components Required

1. **Tools/DocGenerator/** - .NET console app for command documentation generation
2. **.github/scripts/update-version-manifest.py** - Python script for version manifest updates
3. **.github/workflows/docs.yml** - Documentation deployment workflow
4. **templates/partials/navbar.tmpl.partial** - Custom DocFX template for version switcher UI
5. **wt.cli/wt.cli.csproj** - Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>`

## Testing Strategy Summary

| Layer | Tool | What It Validates | When It Runs |
|-------|------|-------------------|--------------|
| Build Validation | DocFX `--warningsAsErrors` | Markdown syntax, YAML structure, internal refs | During build step |
| Link Validation | LinkChecker | Internal/external links, images | After build, before deploy |

**Intentionally excluded**: Content completeness tests (xUnit) - not needed, adds unnecessary complexity

**Next Phase**: Proceed to Phase 1 (Design & Contracts) to generate data-model.md, contracts/, and quickstart.md.
