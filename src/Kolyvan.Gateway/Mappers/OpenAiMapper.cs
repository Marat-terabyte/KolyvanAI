using Kolyvan.Gateway.WebModels.Chat;
using Kolyvan.Gateway.Exceptions;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

namespace Kolyvan.Gateway.Mappers;

public static class OpenAiMapper
{
    public static ToolCallDto ToToolCall(FunctionCallContent fc) => new()
    {
        Id = fc.CallId,
        Function = new ToolCallFunctionDto
        {
            Name = fc.Name,
            Arguments = JsonSerializer.Serialize(fc.Arguments ?? new Dictionary<string, object?>())
        }
    };

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
                "tool" => ChatRole.Tool,
                _ => throw new OpenAiRequestException(
                    $"Unsupported role '{m.Role}'.", $"messages[{i}].role")
            };

            ChatMessage msg;

            if (m.Role == "tool")
            {
                if (string.IsNullOrEmpty(m.ToolCallId))
                {
                    throw new OpenAiRequestException("'tool_call_id' is required.", $"messages[{i}].tool_call_id");
                }

                msg = new ChatMessage(ChatRole.Tool, [new FunctionResultContent(m.ToolCallId, ReadText(m.Content, i))]);
            }
            else
            {
                var contents = new List<AIContent>();
                var text = ReadText(m.Content, i);
                if (text.Length > 0)
                {
                    contents.Add(new TextContent(text));
                }

                foreach (var call in m.ToolCalls ?? [])
                {
                    var args = string.IsNullOrWhiteSpace(call.Function?.Arguments) ? null
                        : JsonSerializer.Deserialize<Dictionary<string, object?>>(call.Function.Arguments);

                    contents.Add(new FunctionCallContent(call.Id ?? "", call.Function?.Name ?? "", args));
                }

                msg = new ChatMessage(role, contents) { AuthorName = m.Name };
            }

            result.Add(msg);
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
        StopSequences = ReadStop(request.Stop),
        Tools = request.Tools?
            .Where(t => t.Function?.Name is not null)
            .Select(t => (AITool)AIFunctionFactory.CreateDeclaration(
                t.Function!.Name!,
                t.Function.Description,
                t.Function.Parameters ?? JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement))
            .ToList(),
        ToolMode = ReadToolChoice(request.ToolChoice)
    };
    private static ChatToolMode? ReadToolChoice(JsonElement? choice) => choice switch
    {
        null => null,
        { ValueKind: JsonValueKind.String } c => c.GetString() switch
        {
            "none" => ChatToolMode.None,
            "required" => ChatToolMode.RequireAny,
            _ => ChatToolMode.Auto
        },
        { ValueKind: JsonValueKind.Object } c => ChatToolMode.RequireSpecific(
            c.GetProperty("function").GetProperty("name").GetString()!),
        _ => null
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
