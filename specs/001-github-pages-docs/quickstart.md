# Quick Start: GitHub Pages Documentation Publishing

**Feature**: 001-github-pages-docs  
**Audience**: Developers implementing this feature  
**Est. Time**: 4-6 hours for initial setup

This guide provides step-by-step instructions to implement automated documentation publishing to GitHub Pages.

## Prerequisites

- Repository with .NET 10.0 project (`wt.cli`)
- GitHub repository with Actions enabled
- Existing `docfx.json` configuration
- Git branching strategy established

## Implementation Phases

### Phase 1: Enable XML Documentation (15 minutes)

#### Step 1.1: Update .csproj

Edit `wt.cli/wt.cli.csproj`:

```xml
<PropertyGroup>
  <!-- Existing properties -->
  <TargetFramework>net10.0</TargetFramework>
  
  <!-- Add XML documentation generation -->
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <DocumentationFile>bin\$(Configuration)\$(TargetFramework)\wt.xml</DocumentationFile>
  
  <!-- Optional: Suppress missing XML comment warnings during development -->
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

#### Step 1.2: Add XML Documentation Comments

Add XML comments to public APIs:

```csharp
/// <summary>
/// Creates a new Git worktree for the specified branch.
/// </summary>
/// <param name="branchName">Name of the branch to create worktree for.</param>
/// <param name="path">Optional custom path for the worktree directory.</param>
/// <returns>Exit code: 0 for success, 1 for failure.</returns>
public static int CreateWorktree(string branchName, string? path = null)
{
    // Implementation
}
```

#### Step 1.3: Verify Build

```bash
cd wt.cli
dotnet build --configuration Release
# Verify wt.xml is generated in bin/Release/net10.0/
ls bin/Release/net10.0/wt.xml
```

**Expected Output**: XML file with API documentation exists.

---

### Phase 2: Create Command Documentation Generator (1-2 hours)

#### Step 2.1: Create Project Structure

```bash
mkdir -p Tools/DocGenerator
cd Tools/DocGenerator
dotnet new console
dotnet add package System.CommandLine --version 2.0.2
```

#### Step 2.2: Implement Generator

Create `Tools/DocGenerator/Program.cs`:

```csharp
using System;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.IO;
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

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: DocGenerator <output-directory>");
            return 1;
        }
        
        var outputDir = args[0];
        var commandsDir = Path.Combine(outputDir, "commands");
        Directory.CreateDirectory(commandsDir);
        
        // Create RootCommand with same structure as wt.cli
        var rootCommand = BuildCliStructure();
        
        // Generate documentation
        GenerateCommandDocs(rootCommand, outputDir);
        
        Console.WriteLine($"✅ Generated documentation for {rootCommand.Subcommands.Count} commands");
        return 0;
    }
    
    static RootCommand BuildCliStructure()
    {
        // TODO: Import actual command structure from wt.cli
        // For now, create example structure
        var rootCommand = new RootCommand("Git worktree manager");
        
        var createCommand = new Command("create", "Create a new worktree");
        createCommand.AddOption(new Option<string>("--path", "Custom path for worktree"));
        createCommand.AddOption(new Option<string>("--editor", "Editor to open"));
        rootCommand.AddSubcommand(createCommand);
        
        var listCommand = new Command("list", "List all worktrees");
        listCommand.AddOption(new Option<bool>("--json", "Output in JSON format"));
        rootCommand.AddSubcommand(listCommand);
        
        return rootCommand;
    }
    
    static void GenerateCommandDocs(RootCommand root, string outputDir)
    {
        var overview = new StringBuilder();
        overview.AppendLine("# Command Reference");
        overview.AppendLine();
        overview.AppendLine("Complete reference for all `wt` commands.");
        overview.AppendLine();
        
        foreach (var command in root.Subcommands)
        {
            var markdown = ConvertToMarkdown(command);
            var fileName = $"{command.Name}.md";
            File.WriteAllText(Path.Combine(outputDir, "commands", fileName), markdown);
            
            overview.AppendLine($"- [`wt {command.Name}`](commands/{command.Name}.md) - {command.Description}");
        }
        
        File.WriteAllText(Path.Combine(outputDir, "command-reference.md"), overview.ToString());
    }
    
    static string ConvertToMarkdown(Command command)
    {
        var md = new StringBuilder();
        md.AppendLine($"# wt {command.Name}");
        md.AppendLine();
        md.AppendLine(command.Description);
        md.AppendLine();
        
        md.AppendLine("## Usage");
        md.AppendLine("```bash");
        md.AppendLine($"wt {command.Name} [options]");
        md.AppendLine("```");
        md.AppendLine();
        
        if (command.Options.Any())
        {
            md.AppendLine("## Options");
            md.AppendLine();
            foreach (var option in command.Options)
            {
                md.AppendLine($"### `{string.Join(", ", option.Aliases)}`");
                md.AppendLine();
                md.AppendLine(option.Description ?? "");
                md.AppendLine();
            }
        }
        
        return md.ToString();
    }
}
```

#### Step 2.3: Test Locally

```bash
cd Tools/DocGenerator
dotnet run -- ../../docs
# Verify generated files
ls ../../docs/commands/
cat ../../docs/command-reference.md
```

**Expected Output**: Markdown files created for each command.

---

### Phase 3: Create Version Manifest Script (30 minutes)

#### Step 3.1: Create Python Script

Create `.github/scripts/update-version-manifest.py`:

```python
#!/usr/bin/env python3
"""Update version manifest JSON file with new documentation version."""
import json
import argparse
import sys

def update_manifest(input_file, output_file, new_version, release_date):
    """Update manifest with new version, marking it as latest."""
    
    try:
        with open(input_file, 'r') as f:
            manifest = json.load(f)
    except FileNotFoundError:
        manifest = {'versions': []}
    
    # Remove "latest" from all versions
    for v in manifest['versions']:
        v['isLatest'] = False
        v['label'] = v['label'].replace(' (latest)', '')
    
    # Check if version exists
    existing = next((v for v in manifest['versions'] if v['path'] == new_version), None)
    
    if existing:
        existing['released'] = release_date
        existing['isLatest'] = True
        existing['label'] = f"{new_version} (latest)"
        print(f"✅ Updated existing version: {new_version}")
    else:
        manifest['versions'].insert(0, {
            'label': f"{new_version} (latest)",
            'path': new_version,
            'released': release_date,
            'isLatest': True
        })
        print(f"✅ Added new version: {new_version}")
    
    # Sort by date
    manifest['versions'].sort(key=lambda v: v['released'], reverse=True)
    
    # Write updated manifest
    with open(output_file, 'w') as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)
    
    print(f"   Total versions: {len(manifest['versions'])}")
    return 0

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--version', required=True)
    parser.add_argument('--date', required=True)
    parser.add_argument('--input', default='version-manifest.json')
    parser.add_argument('--output', default='version-manifest.json')
    args = parser.parse_args()
    
    sys.exit(update_manifest(args.input, args.output, args.version, args.date))
```

#### Step 3.2: Make Executable

```bash
chmod +x .github/scripts/update-version-manifest.py
```

#### Step 3.3: Test Locally

```bash
python3 .github/scripts/update-version-manifest.py \
  --version "v0.1" \
  --date "2026-01-15T10:00:00Z"
  
cat version-manifest.json
```

**Expected Output**: Valid JSON with version entry.

---

### Phase 4: Create GitHub Actions Workflow (1 hour)

#### Step 4.1: Create Workflow File

Create `.github/workflows/docs.yml`:

```yaml
name: Deploy Documentation

on:
  release:
    types: [published]
  workflow_dispatch:
    inputs:
      version:
        description: 'Manual version override (e.g., v0.1)'
        required: false
        type: string

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
      
      - name: Build API documentation
        run: |
          cd wt.cli
          dotnet build --configuration Release
      
      - name: Extract version
        id: version
        run: |
          if [ -n "${{ github.event.inputs.version }}" ]; then
            VERSION="${{ github.event.inputs.version }}"
          else
            TAG="${{ github.event.release.tag_name }}"
            VERSION=$(echo $TAG | sed -E 's/^v([0-9]+)\.([0-9]+)\.[0-9]+$/v\1.\2/')
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

#### Step 4.2: Configure GitHub Pages

1. Go to repository Settings → Pages
2. Source: Select "GitHub Actions"
3. Save

#### Step 4.3: Create Environment

1. Go to Settings → Environments
2. Click "New environment"
3. Name: `github-pages`
4. Click "Configure environment"
5. (Optional) Add protection rules

---

### Phase 5: Create Version Switcher UI (30-45 minutes)

#### Step 5.1: Create Custom Template Directory

```bash
mkdir -p templates/partials
```

#### Step 5.2: Create Navbar Override

Create `templates/partials/navbar.tmpl.partial`:

```html
<!-- Existing navbar content -->
<nav class="navbar navbar-inverse navbar-fixed-top" id="navbar">
  <div class="container">
    <!-- Brand and toggle -->
    <div class="navbar-header">
      <button type="button" class="navbar-toggle collapsed" data-toggle="collapse" data-target="#navbar-collapse">
        <span class="sr-only">Toggle navigation</span>
        <span class="icon-bar"></span>
        <span class="icon-bar"></span>
        <span class="icon-bar"></span>
      </button>
      <a class="navbar-brand" href="{{_rel}}index.html">
        <img id="logo" class="svg" src="{{_rel}}logo.svg" alt="{{_appName}}">
      </a>
    </div>
    
    <!-- Navbar collapse -->
    <div class="collapse navbar-collapse" id="navbar-collapse">
      <form class="navbar-form navbar-right" role="search" id="search">
        <div class="form-group">
          <input type="text" class="form-control" id="search-query" placeholder="Search" autocomplete="off">
        </div>
      </form>
      
      <!-- Add version switcher -->
      <div class="navbar-form navbar-right">
        <div class="form-group">
          <label for="version-switcher" style="color: #fff; margin-right: 5px;">Version:</label>
          <select id="version-switcher" class="form-control" style="display: inline-block; width: auto;">
            <option>Loading...</option>
          </select>
        </div>
      </div>
    </div>
  </div>
</nav>

<script>
(function() {
  function switchVersion(targetVersion) {
    const currentPath = window.location.pathname;
    const pathWithoutVersion = currentPath.replace(/\/v\d+\.\d+\//, '/');
    window.location.href = `/${targetVersion}${pathWithoutVersion}`;
  }
  
  fetch('/version-manifest.json')
    .then(res => res.json())
    .then(data => {
      const selector = document.getElementById('version-switcher');
      selector.innerHTML = '';
      selector.onchange = function() { switchVersion(this.value); };
      
      data.versions.forEach(v => {
        const option = document.createElement('option');
        option.value = v.path;
        option.textContent = v.label;
        
        if (window.location.pathname.includes(`/${v.path}/`)) {
          option.selected = true;
        }
        
        selector.appendChild(option);
      });
    })
    .catch(err => {
      console.error('Failed to load versions:', err);
      document.getElementById('version-switcher').parentElement.parentElement.style.display = 'none';
    });
})();
</script>
```

#### Step 5.3: Update docfx.json Template Configuration

Edit `docfx.json`:

```json
{
  "build": {
    "template": ["default", "modern", "templates"]
  }
}
```

---

### Phase 6: Update Documentation Content (1-2 hours)

#### Step 6.1: Create Installation Guide

Create `docs/installation.md`:

```markdown
# Installation

## System Requirements
- **Windows**: Windows 10 or later (x64)
- **macOS**: macOS 11 or later (ARM64/Apple Silicon)
- **Linux**: x64 or ARM architecture

**Note**: No .NET SDK or Git installation required for the `wt` tool itself.

## Download

Download from [latest release](https://github.com/kuju63/wt/releases/latest):
- **Windows**: `wt-win-x64.zip`
- **macOS**: `wt-osx-arm64.tar.gz`
- **Linux (x64)**: `wt-linux-x64.tar.gz`

## Windows Installation

1. Download `wt-win-x64.zip`
2. Extract to `C:\Program Files\wt\`
3. Add to PATH:
   ```powershell
   setx PATH "%PATH%;C:\Program Files\wt"
   ```

1. Verify: `wt --version`

## macOS Installation

1. Download `wt-osx-arm64.tar.gz`
2. Extract and install:

   ```bash
   tar -xzf wt-osx-arm64.tar.gz
   sudo mv wt /usr/local/bin/
   sudo chmod +x /usr/local/bin/wt
   ```

3. Verify: `wt --version`

## Linux Installation

1. Download appropriate archive
2. Extract and install:

   ```bash
   tar -xzf wt-linux-x64.tar.gz
   sudo mv wt /usr/local/bin/
   sudo chmod +x /usr/local/bin/wt
   ```

3. Verify: `wt --version`

```

#### Step 6.2: Create Contribution Guide
Create `docs/contributing.md`:

```markdown
# Contributing to wt

## Development Environment Setup

**Prerequisites for Development**:
- .NET 10.0 SDK or later
- Git 2.5 or later

### Setup Steps
1. Clone: `git clone https://github.com/kuju63/wt.git`
2. Restore: `dotnet restore`
3. Build: `dotnet build`
4. Test: `dotnet test`

## Coding Standards
See [Constitution](../.specify/memory/constitution.md):
- TDD required
- Minimal dependencies
- Clean and secure code

## Pull Request Process
1. Create feature branch
2. Write tests first
3. Implement feature
4. Ensure tests pass
5. Submit PR
```

#### Step 6.3: Update docs/toc.yml

Edit `docs/toc.yml`:

```yaml
- name: Home
  href: index.md
- name: Installation
  href: installation.md
- name: Command Reference
  href: command-reference.md
- name: API Reference
  href: ../api/
- name: Contributing
  href: contributing.md
```

---

### Phase 7: Testing and Verification (30-45 minutes)

#### Step 7.1: Local Build Test

```bash
# Generate command docs
cd Tools/DocGenerator
dotnet run -- ../../docs
cd ../..

# Build API docs
cd wt.cli
dotnet build --configuration Release
cd ..

# Build DocFX
docfx build docfx.json

# Check output
ls _site/
open _site/index.html  # or xdg-open on Linux
```

#### Step 7.2: Commit and Push

```bash
git checkout -b 001-github-pages-docs
git add .
git commit -m "feat: add GitHub Pages documentation publishing"
git push origin 001-github-pages-docs
```

#### Step 7.3: Create PR and Merge

1. Create pull request
2. Review changes
3. Merge to main

#### Step 7.4: Create Release

1. Go to Releases → Draft a new release
2. Tag: `v0.1.0`
3. Title: `v0.1.0 - Initial Release`
4. Publish release
5. Watch Actions tab for workflow execution

#### Step 7.5: Verify Deployment

After workflow completes:

1. Visit `https://{username}.github.io/wt/v0.1/`
2. Check version switcher loads
3. Test navigation between pages
4. Verify API reference exists
5. Test search functionality

---

## Troubleshooting

### Issue: XML Documentation Not Generated

**Solution**: Verify `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in `.csproj`

### Issue: DocFX Build Fails with "File not found"

**Solution**: Check paths in `docfx.json` are relative to the config file location

### Issue: Version Switcher Not Showing

**Solution**: Check browser console for errors, verify `/version-manifest.json` is accessible

### Issue: Link Validation Fails

**Solution**: Fix broken links in markdown files, ensure all referenced files exist

### Issue: GitHub Pages Deployment Permission Denied

**Solution**: Verify `github-pages` environment exists and has correct permissions

---

## Success Checklist

- ✅ XML documentation enabled and building
- ✅ Command documentation generator working
- ✅ Version manifest script tested
- ✅ GitHub Actions workflow created
- ✅ Version switcher UI implemented
- ✅ Installation guide written
- ✅ Contribution guide written
- ✅ Local DocFX build succeeds
- ✅ First release published
- ✅ Documentation deployed to GitHub Pages
- ✅ Version switcher functional
- ✅ All links working
- ✅ Search functional

---

## Next Steps

After successful first release:

1. **Monitor Analytics**: Track documentation usage
2. **Gather Feedback**: From users on documentation clarity
3. **Iterate**: Improve based on feedback
4. **Document Process**: Update this quickstart based on experience

**Estimated Total Time**: 4-6 hours for full implementation
