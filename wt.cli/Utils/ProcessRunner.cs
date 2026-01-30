using System.Diagnostics;

namespace Kuju63.WorkTree.CommandLine.Utils;

/// <summary>
/// Represents the result of a process execution.
/// </summary>
public record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError
);

/// <summary>
/// Provides functionality for running external processes.
/// </summary>
public class ProcessRunner : IProcessRunner
{
    /// <summary>
    /// Runs an external process asynchronously. This overload does not accept a working directory or cancellation token.
    /// </summary>
    /// <param name="command">The command or executable to run.</param>
    /// <param name="arguments">The arguments to pass to the command.</param>
    /// <returns>A <see cref="ProcessResult"/> containing the exit code and output.</returns>
    public Task<ProcessResult> RunAsync(string command, string arguments)
        => RunAsync(command, arguments, null, CancellationToken.None);

    /// <summary>
    /// Runs an external process asynchronously with a specified working directory. This overload does not accept a cancellation token.
    /// </summary>
    /// <param name="command">The command or executable to run.</param>
    /// <param name="arguments">The arguments to pass to the command.</param>
    /// <param name="workingDirectory">The working directory for the process. If <see langword="null"/>, uses the current directory.</param>
    /// <returns>A <see cref="ProcessResult"/> containing the exit code and output.</returns>
    public Task<ProcessResult> RunAsync(string command, string arguments, string? workingDirectory)
        => RunAsync(command, arguments, workingDirectory, CancellationToken.None);

    public async Task<ProcessResult> RunAsync(
        string command,
        string arguments,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
        };

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // キャンセレーショントークンの登録
        using var registration = cancellationToken.Register(() =>
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        });

        await process.WaitForExitAsync(cancellationToken);

        return new ProcessResult(
            process.ExitCode,
            outputBuilder.ToString().Trim(),
            errorBuilder.ToString().Trim()
        );
    }
}
