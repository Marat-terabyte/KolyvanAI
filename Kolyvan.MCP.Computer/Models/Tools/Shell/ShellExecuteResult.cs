using System.ComponentModel;

namespace Kolyvan.MCP.Computer.Models.Tools.Shell;

/// <summary>
/// Result from executing a shell command.
/// </summary>
[Description("Result from executing a shell command.")]
public class ShellExecuteResult
{
    /// <summary>
    /// Exit code of the process.
    /// </summary>
    [Description("Exit code of the executed process.")]
    public int ExitCode { get; set; }

    /// <summary>
    /// Standard output from the process.
    /// </summary>
    [Description("Standard output from the process.")]
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Standard error from the process.
    /// </summary>
    [Description("Standard error from the process.")]
    public string Error { get; set; } = string.Empty;
}
