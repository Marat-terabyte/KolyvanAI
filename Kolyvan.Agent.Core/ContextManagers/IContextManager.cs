using Microsoft.Extensions.AI;

namespace Kolyvan.Agent.Core.ContextManagers;

/// <summary>
/// Мененджер для работы с контекстом
/// </summary>
public interface IContextManager
{
    /// <summary>
    /// Добавление сообщения в контекст
    /// </summary>
    public Task AddMessageAsync(ChatMessage chatMessage, CancellationToken cancellationToken);

    /// <summary>
    /// Добавление сообщений в контекст
    /// </summary>
    public Task AddMessagesAsync(IEnumerable<ChatMessage> chatMessages, CancellationToken cancellationToken);

    /// <summary>
    /// Получение сообщений из контекста
    /// </summary>
    public Task<List<ChatMessage>> GetChatMessagesAsync(CancellationToken cancellationToken);
}
