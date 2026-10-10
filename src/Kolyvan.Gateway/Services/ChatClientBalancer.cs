using Kolyvan.Gateway.Interfaces;
using Microsoft.Extensions.AI;

namespace Kolyvan.Gateway.Services;

public class ChatClientBalancer : IChatClient
{
    private readonly IEnumerable<IBackendChatClient> _chatClients;

    public ChatClientBalancer(IEnumerable<IBackendChatClient> chatClients)
    {
        _chatClients = chatClients;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
