using Kolyvan.Gateway.Interfaces;
using Microsoft.Extensions.AI;

namespace Kolyvan.Gateway.Services;

public class ChatClientBalancer : IChatClient
{
    private const string SystemPrompt = "Ты - Колыван AI";
    private readonly ChatMessage _systemMessage = new ChatMessage(ChatRole.System, SystemPrompt);

    private readonly IReadOnlyList<IBackendChatClient> _chatClients;

    private int _index = 0;
    
    public ChatClientBalancer(IEnumerable<IBackendChatClient> chatClients)
    {
        _chatClients = chatClients.ToList();
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        IChatClient client = GetChatClient();

        return client.GetResponseAsync([_systemMessage, ..messages], options, cancellationToken);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        IChatClient client = GetChatClient();

        return client.GetStreamingResponseAsync([_systemMessage, .. messages], options, cancellationToken);
    }

    private IChatClient GetChatClient()
    {
        int i = GetIndex();

        IChatClient client = _chatClients[i];

        return client;
    }

    private int GetIndex()
    {
        if (_index >= _chatClients.Count)
        {
            _index = 0;
        }

        _index++;

        return _index - 1;
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    public void Dispose()
    {
        foreach (var chatClient in _chatClients)
        {
            chatClient.Dispose();
        }
    }
}
