using Kolyvan.Agent.Core.Tools;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace Kolyvan.Agent.Console;

internal class ToolRepository : IToolRepository
{
    private readonly McpClient _mcpClient;
    private IList<AIFunction>? _aiFunctions = null;

    public ToolRepository(McpClient mcpClient)
    {
        _mcpClient = mcpClient;
    }

    public async Task<IList<AITool>> GetToolsAsync(CancellationToken cancellationToken)
    {
        IList<McpClientTool> tools = await _mcpClient.ListToolsAsync(cancellationToken: cancellationToken);

        _aiFunctions = (IList<AIFunction>)tools;

        return (IList<AITool>) tools;
    }

    public async Task<object?> InvokeToolAsync(string name, IDictionary<string, object?>? arguments, CancellationToken cancellationToken)
    {
        if (_aiFunctions == null)
        {
            await GetToolsAsync(cancellationToken);
        }

        AIFunction? aiFunction = _aiFunctions.FirstOrDefault(x => x.Name == name);
        if (aiFunction == null)
        {
            throw new Exception();
        }

        return await aiFunction.InvokeAsync(new AIFunctionArguments(arguments), cancellationToken);
    }
}
