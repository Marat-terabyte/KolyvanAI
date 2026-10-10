using Microsoft.Extensions.AI;

namespace Kolyvan.Agent.Core.Tools;

/// <summary>
/// Репозиторий инструментов
/// </summary>
public interface IToolRepository
{
    /// <summary>
    /// Получение всех инструментов
    /// </summary>
    public Task<IList<AITool>> GetToolsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Вызов инструмента
    /// </summary>
    public Task<object?> InvokeToolAsync(string name, IDictionary<string, object?>? arguments,  CancellationToken cancellationToken);
}
