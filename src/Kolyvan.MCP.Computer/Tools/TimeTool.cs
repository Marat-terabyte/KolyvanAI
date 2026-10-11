using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Kolyvan.MCP.Computer.Tools
{
    [McpServerToolType]
    public class TimeTool
    {
        [Description("Get current UTC time")]
        public static DateTime GetCurrentTime() => DateTime.UtcNow;
    }
}
