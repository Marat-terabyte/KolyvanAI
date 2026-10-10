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
    [JsonPropertyName("content")] public required string Content { get; init; }
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
