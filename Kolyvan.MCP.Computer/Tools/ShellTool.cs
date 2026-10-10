using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using Kolyvan.MCP.Computer.Models.Tools.Shell;

namespace Kolyvan.MCP.Computer.Tools;

[McpServerToolType]
[Description("Shell execution tools for running console commands.")]
public class ShellTool
{
    [McpServerTool]
    [Description("""
    Executes a Windows command using cmd.exe.

    The command runs in the MCP server process environment,
    which may differ from the user's interactive terminal.

    Do not assume that a command is available.
    If necessary, verify it with `where <command>`.

    Do not assume the working directory.
    Verify it when relevant.

    Use Windows/cmd.exe syntax.
    """)]
    public async Task<ShellExecuteResult> Execute(
        [Description("Command to execute.")]
        string command,

        [Description("Working directory. If omitted, the process current directory is used.")]
        string? workingDirectory = null,

        CancellationToken cancellationToken = default)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add(command);

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }
        
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Failed to start cmd.exe.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken);

        return new ShellExecuteResult
        {
            ExitCode = process.ExitCode,
            Output = await stdoutTask,
            Error = await stderrTask
        };
    }
}
