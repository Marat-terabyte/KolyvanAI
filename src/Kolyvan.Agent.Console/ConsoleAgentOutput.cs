using Kolyvan.Agent.Core.Outputs;

namespace Kolyvan.Agent.Console;

public class ConsoleAgentOutput : IAgentOutput
{
    public Task OnAssistantTextAsync(string message, CancellationToken cancellationToken)
    {
        System.Console.Write(message);

        return Task.CompletedTask;
    }

    public Task OnToolCallAsync(string toolName, IDictionary<string, object?>? arguments, CancellationToken cancellationToken)
    {
        System.Console.Write($"[TOOL]: {toolName}");

        return Task.CompletedTask;
    }

    public Task OnToolResultAsync(string toolName, object? result, CancellationToken cancellationToken)
    {
        System.Console.Write($"[TOOL]: Completed tool '{toolName}'");

        return Task.CompletedTask;
    }
}
