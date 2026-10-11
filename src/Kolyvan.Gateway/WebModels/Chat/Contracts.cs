using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kolyvan.Gateway.WebModels.Chat;

public sealed class ChatCompletionRequest
{
    [JsonPropertyName("model")] public string? Model { get; init; }
    [JsonPropertyName("messages")] public List<RequestMessage>? Messages { get; init; }
    [JsonPropertyName("stream")] public bool? Stream { get; init; }
    [JsonPropertyName("stream_options")] public StreamOptions? StreamOptions { get; init; }

    [JsonPropertyName("temperature")] public float? Temperature { get; init; }
    [JsonPropertyName("top_p")] public float? TopP { get; init; }
    [JsonPropertyName("max_tokens")] public int? MaxTokens { get; init; }
    [JsonPropertyName("max_completion_tokens")] public int? MaxCompletionTokens { get; init; }
    [JsonPropertyName("frequency_penalty")] public float? FrequencyPenalty { get; init; }
    [JsonPropertyName("presence_penalty")] public float? PresencePenalty { get; init; }
    [JsonPropertyName("seed")] public long? Seed { get; init; }

    [JsonPropertyName("tools")] public List<ToolDto>? Tools { get; init; }
    /// <summary>"auto" | "none" | "required" | { "type":"function", "function": { "name": "..." } }</summary>
    [JsonPropertyName("tool_choice")] public JsonElement? ToolChoice { get; init; }

    /// <summary>string | string[]</summary>
    [JsonPropertyName("stop")] public JsonElement? Stop { get; init; }
}

public sealed class StreamOptions
{
    [JsonPropertyName("include_usage")] public bool? IncludeUsage { get; init; }
}

public sealed class RequestMessage
{
    [JsonPropertyName("role")] public string? Role { get; init; }

    /// <summary>string | [{ "type": "text", "text": "..." }]</summary>
    [JsonPropertyName("content")] public JsonElement? Content { get; init; }

    [JsonPropertyName("name")] public string? Name { get; init; }

    [JsonPropertyName("tool_calls")] public List<ToolCallDto>? ToolCalls { get; init; }
    [JsonPropertyName("tool_call_id")] public string? ToolCallId { get; init; }
}

public sealed class ToolDto
{
    [JsonPropertyName("type")] public string? Type { get; init; }
    [JsonPropertyName("function")] public ToolFunctionDto? Function { get; init; }
}

public sealed class ToolFunctionDto
{
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    /// <summary>JSON Schema</summary>
    [JsonPropertyName("parameters")] public JsonElement? Parameters { get; init; }
}

public sealed class ToolCallDto
{
    [JsonPropertyName("id")] public string? Id { get; init; }
    [JsonPropertyName("type")] public string Type { get; init; } = "function";
    [JsonPropertyName("function")] public ToolCallFunctionDto? Function { get; init; }
}

public sealed class ToolCallFunctionDto
{
    [JsonPropertyName("name")] public string? Name { get; init; }
    /// <summary>JSON-строка, а не объект</summary>
    [JsonPropertyName("arguments")] public string? Arguments { get; init; }
}

// ---------- Response (non-streaming) ----------

public sealed class ChatCompletionResponse
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("object")] public string ObjectType { get; init; } = "chat.completion";
    [JsonPropertyName("created")] public required long Created { get; init; }
    [JsonPropertyName("model")] public required string Model { get; init; }
    [JsonPropertyName("choices")] public required List<ResponseChoice> Choices { get; init; }

    [JsonPropertyName("usage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UsageDto? Usage { get; init; }
}

public sealed class ResponseChoice
{
    [JsonPropertyName("index")] public int Index { get; init; }
    [JsonPropertyName("message")] public required ResponseMessage Message { get; init; }
    [JsonPropertyName("finish_reason")] public required string FinishReason { get; init; }
}

public sealed class ResponseMessage
{
    [JsonPropertyName("role")] public string Role { get; init; } = "assistant";
    [JsonPropertyName("content")] public string? Content { get; init; }
    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ToolCallDto>? ToolCalls { get; init; }
}

// ---------- Response (streaming chunks) ----------

public sealed class ChatCompletionChunk
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("object")] public string ObjectType { get; init; } = "chat.completion.chunk";
    [JsonPropertyName("created")] public required long Created { get; init; }
    [JsonPropertyName("model")] public required string Model { get; init; }
    [JsonPropertyName("choices")] public required List<ChunkChoice> Choices { get; init; }

    [JsonPropertyName("usage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UsageDto? Usage { get; init; }
}

public sealed class ChunkChoice
{
    [JsonPropertyName("index")] public int Index { get; init; }
    [JsonPropertyName("delta")] public required ChunkDelta Delta { get; init; }

    // Намеренно без Ignore: OpenAI отдаёт "finish_reason": null в промежуточных чанках.
    [JsonPropertyName("finish_reason")] public string? FinishReason { get; init; }
}

public sealed class ChunkDelta
{
    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Role { get; init; }

    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; init; }

    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ToolCallChunkDto>? ToolCalls { get; init; }
}
public sealed class ToolCallChunkDto
{
    [JsonPropertyName("index")] public int Index { get; init; }
    [JsonPropertyName("id")] public string? Id { get; init; }
    [JsonPropertyName("type")] public string Type { get; init; } = "function";
    [JsonPropertyName("function")] public ToolCallFunctionDto? Function { get; init; }
}

public sealed class UsageDto
{
    [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; init; }
    [JsonPropertyName("completion_tokens")] public int CompletionTokens { get; init; }
    [JsonPropertyName("total_tokens")] public int TotalTokens { get; init; }
}

// ---------- Errors ----------

public sealed class ErrorResponse
{
    [JsonPropertyName("error")] public required ErrorBody Error { get; init; }
}

public sealed class ErrorBody
{
    [JsonPropertyName("message")] public required string Message { get; init; }
    [JsonPropertyName("type")] public required string Type { get; init; }

    [JsonPropertyName("param")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Param { get; init; }

    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Code { get; init; }
}
