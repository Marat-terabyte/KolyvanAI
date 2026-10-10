namespace Kolyvan.Gateway.Exceptions;

public sealed class OpenAiRequestException(string message, string? param = null) : Exception(message)
{
    public string? Param { get; } = param;
}