using Kolyvan.Gateway.Services;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Kolyvan.Gateway.Extensions.DI;

public static class ChatClientDiExtension
{
    public static void AddChatClients(this IServiceCollection services, ConfigurationManager configuration)
    {
        string token = "";
        IChatClient externalChatClient = new ChatClient(
            model: "anthropic/claude-haiku-5-5",
            credential: new ApiKeyCredential(token),
            options: new OpenAIClientOptions
            {
                Endpoint = new Uri("https://api.timeweb.ai/v1"),
            }
        ).AsIChatClient();

        IChatClient localClient = new OllamaApiClient(uri: new Uri("http://192.168.1.104:11434"), defaultModel: "hf.co/HauhauCS/Gemma-4-E4B-Uncensored-HauhauCS-Aggressive:Q4_K_M");

        BackendChatClient externalBackendClient = new BackendChatClient(externalChatClient);
        BackendChatClient localBackendClient = new BackendChatClient(localClient);

        services.AddSingleton<BackendChatClient>(externalBackendClient);
        services.AddSingleton<BackendChatClient>(localBackendClient);

        services.AddSingleton<IChatClient>(x => new ChatClientBalancer(x.GetRequiredService<IEnumerable<BackendChatClient>>()));
    }
}
