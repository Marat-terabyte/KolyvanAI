using Microsoft.Extensions.AI;

namespace Kolyvan.Agent.Core.Outputs;

/// <summary>
/// Вывод чата с ассистентом
/// </summary>
public interface IAgentOutput
{
    public Task OnToolCallAsync(string toolName, IDictionary<string, object?>? arguments, CancellationToken cancellationToken);

    public Task OnToolResultAsync(string toolName, object? result, CancellationToken cancellationToken);

    public Task OnAssistantTextAsync(string message, CancellationToken cancellationToken);
}
