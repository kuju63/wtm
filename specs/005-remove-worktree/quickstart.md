# Quickstart: Remove Git Worktree Feature

**For**: Developers implementing the remove worktree feature

## Overview

This feature adds a new `wt remove` command to safely delete git worktrees and their associated working directories from disk.

**Key Behavior**:

- Validates worktree before removal (not found, main worktree, current worktree)
- Prevents removal of worktrees with uncommitted changes (unless --force)
- Gracefully handles partial disk failures
- No interactive confirmation prompts
- Cross-platform compatible

## Architecture Summary

### Command Entry Point

**File**: `wt.cli/Commands/Worktree/RemoveCommand.cs`

- Inherits System.CommandLine command pattern
- Parses arguments: `<worktree>`, `--force`, `--output`, `--verbose`
- Delegates to WorktreeService.RemoveWorktreeAsync()
- Formats and outputs result

### Service Layer

**File**: `wt.cli/Services/Worktree/WorktreeService.cs`

- Extends existing IWorktreeService interface
- Adds RemoveWorktreeAsync() method
- Adds ValidateForRemoval() validation method
- Uses IGitService for git metadata operations
- Uses IFileSystem for directory deletion

### Data Models

**Files**:

- `wt.cli/Models/RemoveWorktreeOptions.cs` - CLI options binding
- `wt.cli/Models/RemoveWorktreeResult.cs` - Result structure
- `wt.cli/Models/RemovalValidationError.cs` - Enum for validation errors

### Tests

**Files**:

- `wt.tests/Commands/Worktree/RemoveCommandTests.cs` - Command tests
- `wt.tests/Services/Worktree/WorktreeServiceRemoveTests.cs` - Service tests

## Implementation Checklist

### Phase 1: Models & Interfaces

- [ ] Create `RemoveWorktreeOptions` class with WorktreeId, Force, Verbose, OutputFormat
- [ ] Create `RemoveWorktreeResult` and `RemoveWorktreeData` classes extending CommandResult<T>
- [ ] Create `DeletionFailure` class for partial failure reporting
- [ ] Create `RemovalValidationError` enum
- [ ] Update `IWorktreeService` interface:
  - [ ] Add `ValidateForRemoval(string worktreeId, bool force): RemovalValidationError`
  - [ ] Add `RemoveWorktreeAsync(RemoveWorktreeOptions options): Task<RemoveWorktreeResult>`

### Phase 2: Service Implementation

- [ ] Implement `WorktreeService.ValidateForRemoval()`:
  - [ ] Check worktree exists
  - [ ] Check not main worktree
  - [ ] Check not current worktree
  - [ ] Check for uncommitted changes (unless force)
  - [ ] Check for locks (unless force)

- [ ] Implement `WorktreeService.RemoveWorktreeAsync()`:
  - [ ] Call ValidateForRemoval()
  - [ ] Return error if validation fails
  - [ ] Execute `git worktree remove` via GitService
  - [ ] Delete working directory via IFileSystem
  - [ ] Handle UnauthorizedAccessException/IOException
  - [ ] Collect file deletion metrics
  - [ ] Return structured RemoveWorktreeResult

### Phase 3: Command Implementation

- [ ] Create `RemoveCommand` class:
  - [ ] Add Command argument for worktree ID
  - [ ] Add --force option
  - [ ] Add --verbose option
  - [ ] Add --output option {human, json}
  - [ ] Implement Handler that calls RemoveWorktreeAsync()
  - [ ] Format output based on --output flag

- [ ] Register RemoveCommand in Program.cs

### Phase 4: Testing

- [ ] Write RemoveCommandTests:
  - [ ] Test parsing valid arguments
  - [ ] Test error messages for invalid inputs
  - [ ] Test output formatting (human vs JSON)
  - [ ] Test --force flag behavior

- [ ] Write WorktreeServiceRemoveTests:
  - [ ] Test validation: not found
  - [ ] Test validation: main worktree
  - [ ] Test validation: current worktree
  - [ ] Test validation: uncommitted changes
  - [ ] Test validation: success path
  - [ ] Mock IGitService.HasUncommittedChanges()
  - [ ] Mock IFileSystem for directory deletion
  - [ ] Test partial failure scenario
  - [ ] Test error message formatting
  - [ ] Ensure >80% test coverage

### Phase 5: Documentation & Build

- [ ] Verify RemoveCommand has proper XML documentation comments (all public methods)
- [ ] Build the project: `dotnet build`
- [ ] Run DocGenerator tool to auto-generate command documentation:

  ```bash
  dotnet run --project Tools/DocGenerator/DocGenerator/DocGenerator.csproj
  ```

- [ ] Verify generated documentation includes `wt remove` command reference
- [ ] Update CHANGELOG with new feature entry

## Key Implementation Patterns

### Validation Pattern

```csharp
// In WorktreeService
public RemovalValidationError ValidateForRemoval(string worktreeId, bool force)
{
    // Check exists
    var worktree = _gitService.GetWorktreeInfo(worktreeId);
    if (worktree == null) return RemovalValidationError.NotFound;

    // Check main worktree
    if (IsMainWorktree(worktree)) return RemovalValidationError.IsMainWorktree;

    // Check current worktree
    if (IsCurrentWorktree(worktree)) return RemovalValidationError.IsCurrentWorktree;

    // Check uncommitted changes (unless force)
    if (!force && _gitService.HasUncommittedChanges(worktree.Path))
        return RemovalValidationError.UncommittedChanges;

    return RemovalValidationError.None;
}
```

### Async Removal Pattern

```csharp
// In WorktreeService
public async Task<RemoveWorktreeResult> RemoveWorktreeAsync(RemoveWorktreeOptions options)
{
    var sw = Stopwatch.StartNew();
    var validation = ValidateForRemoval(options.WorktreeId, options.Force);

    if (validation != RemovalValidationError.None)
        return CreateErrorResult(validation);

    // Execute git removal
    var gitResult = _gitService.RemoveWorktree(path, options.Force);
    if (!gitResult.Success)
        return CreateErrorResult(gitResult);

    // Delete directory
    var deletionResult = await DeleteWorktreeDirectoryAsync(options.WorktreeId, gitResult.Path);

    return new RemoveWorktreeResult
    {
        Success = deletionResult.Success,
        Data = new RemoveWorktreeData
        {
            WorktreeId = options.WorktreeId,
            Path = gitResult.Path,
            WorktreeMetadataRemoved = true,
            FilesDeleted = deletionResult.FilesDeleted,
            UndeleteableItems = deletionResult.UndeleteableItems,
            Duration = sw.Elapsed
        }
    };
}
```

### Partial Failure Handling

```csharp
// In WorktreeService
private async Task<DeletionResult> DeleteWorktreeDirectoryAsync(string worktreeId, string path)
{
    var undeleteable = new List<DeletionFailure>();
    int filesDeleted = 0;

    try
    {
        var dir = _fileSystem.DirectoryInfo.New(path);
        foreach (var file in dir.GetFiles("*", SearchOption.AllDirectories))
        {
            try
            {
                file.Delete();
                filesDeleted++;
            }
            catch (UnauthorizedAccessException)
            {
                undeleteable.Add(new DeletionFailure
                {
                    Path = file.FullName,
                    Reason = "Permission denied"
                });
            }
            catch (IOException)
            {
                undeleteable.Add(new DeletionFailure
                {
                    Path = file.FullName,
                    Reason = "File in use"
                });
            }
        }

        // Clean up directory structure (best effort)
        try
        {
            _fileSystem.Directory.Delete(path, recursive: false);
        }
        catch
        {
            // Directory still contains files; leave for user cleanup
        }
    }
    catch (Exception)
    {
        // Directory access failed entirely
    }

    return new DeletionResult(Success: undeleteable.Count == 0, filesDeleted, undeleteable);
}
```

### Output Formatting Pattern

```csharp
// In RemoveCommand Handler
var result = await _worktreeService.RemoveWorktreeAsync(options);

if (options.OutputFormat == "json")
{
    // Return JSON serialized result
    Console.WriteLine(JsonSerializer.Serialize(result));
}
else
{
    // Return human-readable output
    if (result.Success)
        Console.WriteLine($"✓ Worktree '{result.Data.WorktreeId}' removed successfully");
    else
        Console.WriteLine($"✗ Error: {result.ErrorMessage}");
}

return result.Success ? 0 : 1;
```

## Error Handling Rules

1. **Validation errors** return immediately without attempting removal
2. **Git command failures** are logged and reported; directory deletion is attempted anyway if possible
3. **Directory deletion failures** are collected and reported but don't block success status
4. **Partial failures** return Success=false but WorktreeMetadataRemoved=true
5. **Unexpected errors** return RemovalValidationError.UnknownError

## Testing Guidelines

- Mock IGitService.HasUncommittedChanges() to test validation
- Mock IFileSystem for directory deletion tests
- Use Moq to verify correct methods are called with expected parameters
- Test both success and failure paths
- Verify error messages are user-friendly and actionable
- Ensure test coverage exceeds 80% (per constitution)
- Test method LOC should stay <50 lines per constitution

## Cross-Platform Considerations

- Use IFileSystem abstraction for all file operations (handled by existing PathHelper)
- Directory separator normalization (handled by existing PathHelper)
- Path length validation per OS limits (Windows 260, Unix 4096)
- File permission differences (Windows ACL vs Unix chmod)
- Built-in executable distribution for all platforms (handled by existing build matrix)

## Build & Documentation Process

1. Implement RemoveCommand with proper XML documentation comments
2. Build project: `dotnet build`
3. Run DocGenerator tool (auto-generates command reference from XML docs):

   ```bash
   dotnet run --project Tools/DocGenerator/DocGenerator/DocGenerator.csproj
   ```

   - Reflects over RemoveCommand class
   - Extracts XML documentation comments
   - Generates command syntax and argument descriptions
   - Produces user-facing CLI reference documentation
4. Verify generated docs are correct and include all options
5. Commit updated documentation

## Code Quality Standards

Per project constitution:

- **Method LOC**: <50 lines (target), refactor if exceeding
- **Cyclomatic Complexity**: <8 (target for each method)
- **Test Coverage**: >80% overall, >90% for critical paths
- **Code Style**: Match existing project patterns (see CreateCommand)
- **XML Documentation**: All public methods must have doc comments
- **Specific Exception Handling**: Catch specific exceptions, not generic Exception

## Notes

- No integration or E2E tests in this scope (deferred to future CI infrastructure)
- All error codes use WT-RM-### prefix for consistency
- Follow existing code style and conventions (see CreateCommand for reference)
- Add XML documentation comments to all public methods and properties
- Use `_` prefix for private fields, `camelCase` for locals
