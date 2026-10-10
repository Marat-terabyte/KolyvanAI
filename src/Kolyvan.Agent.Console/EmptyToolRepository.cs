using Kolyvan.Agent.Core.Tools;
using Microsoft.Extensions.AI;

namespace Kolyvan.Agent.Console;

public class EmptyToolRepository : IToolRepository
{
    public Task<IList<AITool>> GetToolsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IList<AITool>>([]);
    }

    public Task<object?> InvokeToolAsync(string name, IDictionary<string, object?>? arguments, CancellationToken cancellationToken)
    {
        return Task.FromResult<object?>(null);
    }
}
