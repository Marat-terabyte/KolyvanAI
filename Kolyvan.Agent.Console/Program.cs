using System.ClientModel;
using Kolyvan.Agent.Core.ContextManagers;
using Kolyvan.Agent.Core.Outputs;
using Kolyvan.Agent.Core.Tools;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

namespace Kolyvan.Agent.Console;

public static class Program 
{
    public static async Task Main()
    {
        Core.Agent agent = CreateAgent();
        while (true)
        {
            System.Console.Write(">>>");
            string? userPrompt = System.Console.ReadLine();
            if (string.IsNullOrWhiteSpace(userPrompt))
            {
                continue;
            }

            await agent.PromptAsync(userPrompt, CancellationToken.None);
            System.Console.WriteLine();
        }
    }
    
    private static Core.Agent CreateAgent()
    {
        IChatClient client = CreateClient();
        IContextManager contextManager = new InMemoryContextManager();
        IToolRepository toolRepository = new EmptyToolRepository();
        IAgentOutput agentOutput = new ConsoleAgentOutput();

        return new Core.Agent(client, contextManager, toolRepository, agentOutput);
    }

    private static IChatClient CreateClient()
    {
        System.Console.Write("Api key:");
        string key = System.Console.ReadLine() ?? "";

        var client = new ChatClient(
            model: "anthropic/claude-haiku-5-5",
            credential: new ApiKeyCredential(key),
            options: new OpenAIClientOptions
            {
                Endpoint = new Uri("https://api.timeweb.ai/v1"),
            }
        ).AsIChatClient();
        
        return client;
    }

}
