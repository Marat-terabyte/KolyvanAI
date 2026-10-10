using System.Collections.ObjectModel;
using Microsoft.Extensions.AI;

namespace Kolyvan.Agent.Core.ContextManagers;

public class InMemoryContextManager : IContextManager
{
    private List<ChatMessage> _chatMessages;

    public InMemoryContextManager()
    {
        _chatMessages = new List<ChatMessage>();
    }

    public Task AddMessageAsync(ChatMessage chatMessage, CancellationToken cancellationToken)
    {
        _chatMessages.Add(chatMessage);

        return Task.CompletedTask;
    }

    public Task AddMessagesAsync(IEnumerable<ChatMessage> chatMessages, CancellationToken cancellationToken)
    {
        _chatMessages.AddRange(chatMessages);

        return Task.CompletedTask;
    }

    public Task<ReadOnlyCollection<ChatMessage>> GetChatMessagesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_chatMessages.AsReadOnly());
    }
}
