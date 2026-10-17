using Kolyvan.Agent.Core.Tools;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace Kolyvan.Agent.Console;

public class ToolRepository : IToolRepository
{
    private readonly McpClient _mcpClient;

    private IList<AIFunction>? _aiFunctions;
    
    public ToolRepository(McpClient mcpClient)
    {
        _mcpClient = mcpClient;
    }
    
    public async Task<IList<AITool>> GetToolsAsync(CancellationToken cancellationToken)
    {
        if (_aiFunctions is null)
        {
            _aiFunctions = (await _mcpClient.ListToolsAsync(cancellationToken: cancellationToken)).Cast<AIFunction>().ToList();
        }

        return _aiFunctions.Cast<AITool>().ToList();
    }

    public async Task<object?> InvokeToolAsync(string name, IDictionary<string, object?>? arguments, CancellationToken cancellationToken)
    {
        if (_aiFunctions is null)
        {
            await GetToolsAsync(cancellationToken);
        }

        AIFunction? function = _aiFunctions!.FirstOrDefault(x => x.Name == name);
        if (function is null)
        {
            throw new InvalidOperationException($"Tool '{name}' was not found.");
        }

        AIFunctionArguments functionArguments = new(arguments);
        
        return await function.InvokeAsync(functionArguments, cancellationToken);
    }
}
