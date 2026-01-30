using System.CommandLine;
using System.Text;

namespace DocGenerator;

/// <summary>
/// Command documentation generator for System.CommandLine
/// </summary>
static class Program
{
    static async Task<int> Main(string[] args)
    {
        var outputOption = new Option<string>("--output")
        {
            Description = "Output directory for generated documentation",
            DefaultValueFactory = _ => "docs/commands"
        };

        var wtPathOption = new Option<string>("--wt-path")
        {
            Description = "Path to wt executable",
            DefaultValueFactory = _ => "wt.cli/bin/Release/net10.0/osx-arm64/wt"
        };

        var rootCommand = new RootCommand("Generate command documentation from System.CommandLine definitions");
        rootCommand.Options.Add(outputOption);
        rootCommand.Options.Add(wtPathOption);

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var output = parseResult.GetValue(outputOption) ?? "docs/commands";
            var wtPath = parseResult.GetValue(wtPathOption) ?? "wt.cli/bin/Release/net10.0/osx-arm64/wt";
            await GenerateDocumentation(output, wtPath, cancellationToken);
            return 0;
        });

        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
    }

    static async Task GenerateDocumentation(string outputPath, string wtPath, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Generating command documentation to: {outputPath}");
        Console.WriteLine($"Using wt executable: {wtPath}");
        
        // Create output directory if it doesn't exist
        Directory.CreateDirectory(outputPath);
        
        // Get the list of commands from wt --help
        var commands = await GetCommandsAsync(wtPath, cancellationToken);
        
        // Generate documentation for each command
        foreach (var command in commands)
        {
            Console.WriteLine($"  Generating documentation for: {command}");
            var helpText = await GetCommandHelpAsync(wtPath, command, cancellationToken);
            var markdown = CommandDocGenerator.ConvertHelpToMarkdown(command, helpText);
            var filePath = Path.Combine(outputPath, $"{command}.md");
            await File.WriteAllTextAsync(filePath, markdown, cancellationToken);
            Console.WriteLine($"    Generated: {filePath}");
        }
        
        Console.WriteLine("Command documentation generation complete.");
    }

    static async Task<List<string>> GetCommandsAsync(string wtPath, CancellationToken cancellationToken)
    {
        var result = await RunProcessAsync(wtPath, "--help", cancellationToken);
        var commands = new List<string>();
        
        var lines = result.Split('\n');
        bool inCommandsSection = false;
        
        foreach (var line in lines)
        {
            if (line.Contains("コマンド:") || line.Contains("Commands:"))
            {
                inCommandsSection = true;
                continue;
            }
            
            if (inCommandsSection && !string.IsNullOrWhiteSpace(line))
            {
                // Extract command name from lines like "  create <branch>  Create a new worktree..."
                var trimmed = line.Trim();
                if (trimmed.StartsWith("-") || trimmed.StartsWith("--"))
                {
                    // This is an option, not a command
                    break;
                }
                
                var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    commands.Add(parts[0]);
                }
            }
        }
        
        return commands;
    }

    static async Task<string> GetCommandHelpAsync(string wtPath, string command, CancellationToken cancellationToken)
    {
        return await RunProcessAsync(wtPath, $"{command} --help", cancellationToken);
    }

    static async Task<string> RunProcessAsync(string command, string arguments, CancellationToken cancellationToken)
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        
        return output;
    }
}

/// <summary>
/// Markdown console for formatted output
/// </summary>
class MarkdownConsole
{
    private readonly StringBuilder _builder = new();

    public void WriteHeading(int level, string text)
    {
        _builder.AppendLine($"{new string('#', level)} {text}");
        _builder.AppendLine();
    }

    public void WriteText(string text)
    {
        _builder.AppendLine(text);
        _builder.AppendLine();
    }

    public void WriteCodeBlock(string language, string code)
    {
        _builder.AppendLine($"```{language}");
        _builder.AppendLine(code);
        _builder.AppendLine("```");
        _builder.AppendLine();
    }

    public override string ToString() => _builder.ToString();
}

/// <summary>
/// Command documentation generator
/// </summary>
/// <summary>
/// Parsed help text sections
/// </summary>
record HelpSections(
    string? Description,
    string? Usage,
    List<(string name, string desc)> Arguments,
    List<(string name, string desc)> Options
);

static class CommandDocGenerator
{
    public static string ConvertHelpToMarkdown(string commandName, string helpText)
    {
        var sections = ParseHelpText(helpText);
        return GenerateMarkdown(commandName, sections);
    }

    private static HelpSections ParseHelpText(string helpText)
    {
        var lines = helpText.Split('\n');
        
        string? description = null;
        string? usage = null;
        var arguments = new List<(string name, string desc)>();
        var options = new List<(string name, string desc)>();
        
        string? currentSection = null;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }
            
            currentSection = UpdateCurrentSection(trimmed, currentSection);
            
            ProcessLineForSection(currentSection, trimmed, ref description, ref usage, arguments, options);
        }
        
        return new HelpSections(description, usage, arguments, options);
    }

    private static string? UpdateCurrentSection(string trimmed, string? currentSection)
    {
        if (trimmed.StartsWith("Description:"))
        {
            return "description";
        }
        else if (trimmed.Contains("使用法:") || trimmed.Contains("Usage:"))
        {
            return "usage";
        }
        else if (trimmed.Contains("引数:") || trimmed.Contains("Arguments:"))
        {
            return "arguments";
        }
        else if (trimmed.Contains("オプション:") || trimmed.Contains("Options:"))
        {
            return "options";
        }
        
        return currentSection;
    }

    private static void ProcessLineForSection(
        string? currentSection,
        string trimmed,
        ref string? description,
        ref string? usage,
        List<(string name, string desc)> arguments,
        List<(string name, string desc)> options)
    {
        switch (currentSection)
        {
            case "description":
                ProcessDescriptionLine(trimmed, ref description);
                break;
                
            case "usage":
                ProcessUsageLine(trimmed, ref usage);
                break;
                
            case "arguments":
                ProcessArgumentsLine(trimmed, arguments);
                break;
                
            case "options":
                ProcessOptionsLine(trimmed, options);
                break;
        }
    }

    private static void ProcessDescriptionLine(string trimmed, ref string? description)
    {
        if (!string.IsNullOrEmpty(trimmed))
        {
            description = trimmed;
        }
    }

    private static void ProcessUsageLine(string trimmed, ref string? usage)
    {
        if (trimmed.StartsWith("wt "))
        {
            usage = trimmed;
        }
    }

    private static void ProcessArgumentsLine(string trimmed, List<(string name, string desc)> arguments)
    {
        if (trimmed.StartsWith("<"))
        {
            var parts = trimmed.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                arguments.Add((parts[0], string.Join(" ", parts.Skip(1))));
            }
        }
    }

    private static void ProcessOptionsLine(string trimmed, List<(string name, string desc)> options)
    {
        if (trimmed.StartsWith("-"))
        {
            var parts = trimmed.Split(new[] { "  " }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                options.Add((parts[0], parts[1]));
            }
            else if (parts.Length == 1)
            {
                options.Add((parts[0], ""));
            }
        }
    }

    private static string GenerateMarkdown(string commandName, HelpSections sections)
    {
        var console = new MarkdownConsole();
        
        console.WriteHeading(1, $"wt {commandName}");
        
        if (!string.IsNullOrEmpty(sections.Description))
        {
            console.WriteText(sections.Description);
        }
        
        WriteUsageSection(console, sections.Usage);
        WriteArgumentsSection(console, sections.Arguments);
        WriteOptionsSection(console, sections.Options);
        WriteExamplesSection(console);
        
        return console.ToString();
    }

    private static void WriteUsageSection(MarkdownConsole console, string? usage)
    {
        console.WriteHeading(2, "Syntax");
        if (!string.IsNullOrEmpty(usage))
        {
            console.WriteCodeBlock("bash", usage);
        }
    }

    private static void WriteArgumentsSection(MarkdownConsole console, List<(string name, string desc)> arguments)
    {
        if (arguments.Count > 0)
        {
            console.WriteHeading(2, "Arguments");
            foreach (var (name, desc) in arguments)
            {
                console.WriteText($"**`{name}`**  \n{desc}");
            }
        }
    }

    private static void WriteOptionsSection(MarkdownConsole console, List<(string name, string desc)> options)
    {
        if (options.Count > 0)
        {
            console.WriteHeading(2, "Options");
            foreach (var (name, desc) in options)
            {
                if (name.Contains("--help"))
                {
                    continue;
                }
                console.WriteText($"`{name}`  \n{desc}");
            }
        }
    }

    private static void WriteExamplesSection(MarkdownConsole console)
    {
        console.WriteHeading(2, "Examples");
        console.WriteText("See the command reference documentation for usage examples.");
    }
}
