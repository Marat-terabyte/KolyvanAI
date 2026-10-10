using Kolyvan.Gateway.WebModels.Chat;
using Kolyvan.Gateway.Exceptions;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

namespace Kolyvan.Gateway.Mappers;

public static class OpenAiMapper
{
    public static List<ChatMessage> ToChatMessages(IReadOnlyList<RequestMessage> messages)
    {
        var result = new List<ChatMessage>(messages.Count);

        for (var i = 0; i < messages.Count; i++)
        {
            var m = messages[i];

            var role = m.Role switch
            {
                "system" or "developer" => ChatRole.System,
                "user" => ChatRole.User,
                "assistant" => ChatRole.Assistant,
                _ => throw new OpenAiRequestException(
                    $"Unsupported role '{m.Role}'.", $"messages[{i}].role")
            };

            result.Add(new ChatMessage(role, ReadText(m.Content, i)) { AuthorName = m.Name });
        }

        return result;
    }

    public static ChatOptions ToChatOptions(ChatCompletionRequest request) => new()
    {
        //ModelId = request.Model,
        Temperature = request.Temperature,
        TopP = request.TopP,
        MaxOutputTokens = request.MaxCompletionTokens ?? request.MaxTokens,
        FrequencyPenalty = request.FrequencyPenalty,
        PresencePenalty = request.PresencePenalty,
        Seed = request.Seed,
        StopSequences = ReadStop(request.Stop)
    };

    public static string ToFinishReason(ChatFinishReason? reason)
    {
        if (reason == ChatFinishReason.Length) return "length";
        if (reason == ChatFinishReason.ToolCalls) return "tool_calls";
        if (reason == ChatFinishReason.ContentFilter) return "content_filter";
        return "stop";
    }

    public static UsageDto? ToUsage(UsageDetails? usage)
    {
        if (usage is null) return null;

        var input = (int)(usage.InputTokenCount ?? 0);
        var output = (int)(usage.OutputTokenCount ?? 0);

        return new UsageDto
        {
            PromptTokens = input,
            CompletionTokens = output,
            TotalTokens = (int)(usage.TotalTokenCount ?? input + output)
        };
    }

    private static string ReadText(JsonElement? content, int messageIndex)
    {
        if (content is not { } c) return string.Empty;

        switch (c.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return string.Empty;

            case JsonValueKind.String:
                return c.GetString() ?? string.Empty;

            case JsonValueKind.Array:
                var sb = new StringBuilder();
                foreach (var part in c.EnumerateArray())
                {
                    var type = part.TryGetProperty("type", out var t) ? t.GetString() : null;
                    if (type != "text")
                        throw new OpenAiRequestException(
                            $"Unsupported content part type '{type}'. Only 'text' is supported.",
                            $"messages[{messageIndex}].content");

                    sb.Append(part.GetProperty("text").GetString());
                }
                return sb.ToString();

            default:
                throw new OpenAiRequestException(
                    "'content' must be a string or an array of text parts.",
                    $"messages[{messageIndex}].content");
        }
    }

    private static IList<string>? ReadStop(JsonElement? stop)
    {
        if (stop is not { } s) return null;

        return s.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => [s.GetString()!],
            JsonValueKind.Array => s.EnumerateArray()
                .Select(x => x.GetString() ?? throw new OpenAiRequestException("'stop' must contain strings.", "stop"))
                .ToList(),
            _ => throw new OpenAiRequestException("'stop' must be a string or an array of strings.", "stop")
        };
    }
}
