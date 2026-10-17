using Kolyvan.Gateway.Interfaces;
using Kolyvan.Gateway.Services;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Kolyvan.Gateway.Extensions.DI;

public static class ChatClientDiExtension
{
    public static void AddChatClients(this IServiceCollection services, IConfiguration configuration)
    {
        var models = configuration.GetSection("ChatModels").Get<List<ChatModelConfiguration>>() ?? [];
        if (models.Count == 0)
        {
            throw new InvalidOperationException("At least one ChatModels entry must be configured.");
        }

        foreach (var (model, index) in models.Select((model, index) => (model, index)))
        {
            if (string.IsNullOrWhiteSpace(model.Model) || !Uri.TryCreate(model.Endpoint, UriKind.Absolute, out var endpoint))
            {
                throw new InvalidOperationException($"ChatModels:{index} requires a model name and an absolute endpoint URL.");
            }

            IChatClient client = model.Provider?.ToLowerInvariant() switch
            {
                "openai" => CreateOpenAiClient(model, endpoint, index),
                "ollama" => new OllamaApiClient(uri: endpoint, defaultModel: model.Model),
                _ => throw new InvalidOperationException(
                    $"ChatModels:{index}:Provider must be 'OpenAI' or 'Ollama'.")
            };

            services.AddSingleton<IBackendChatClient>(new BackendChatClient(client));
        }

        services.AddSingleton<IChatClient>(provider => new ChatClientBalancer(provider.GetRequiredService<IEnumerable<IBackendChatClient>>()));
    }

    private static IChatClient CreateOpenAiClient(ChatModelConfiguration model, Uri endpoint, int index)
    {
        if (string.IsNullOrWhiteSpace(model.ApiKey))
        {
            throw new InvalidOperationException($"ChatModels:{index}:ApiKey is required for OpenAI.");
        }

        return new ChatClient(
            model: model.Model!,
            credential: new ApiKeyCredential(model.ApiKey),
            options: new OpenAIClientOptions { Endpoint = endpoint }).AsIChatClient();
    }

    private sealed class ChatModelConfiguration
    {
        public string? Provider { get; set; }
        public string? Endpoint { get; set; }
        public string? Model { get; set; }
        public string? ApiKey { get; set; }
    }
}
