using Kolyvan.Gateway.Exceptions;
using Kolyvan.Gateway.Mappers;
using Kolyvan.Gateway.WebModels.Chat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace Kolyvan.Gateway.Controllers;

[ApiController]
[Route("/v1/chat")]
public sealed class ChatController : ControllerBase
{
    private const string DefaultModelName = "kolyvan";

    private readonly IChatClient _chatClient;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatClient chatClient, ILogger<ChatController> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    [HttpPost("completions")]
    public async Task<IActionResult> CompleteAsync([FromBody] ChatCompletionRequest request, CancellationToken cancellationToken)
    {
        List<ChatMessage> messages;
        ChatOptions options;

        try
        {
            if (request.Messages is not { Count: > 0 })
            {
                throw new OpenAiRequestException("'messages' must be a non-empty array.", "messages");
            }

            messages = OpenAiMapper.ToChatMessages(request.Messages);
            options = OpenAiMapper.ToChatOptions(request);
        }
        catch (OpenAiRequestException ex)
        {
            return InvalidRequest(ex.Message, ex.Param);
        }

        var model = request.Model ?? DefaultModelName;

        return request.Stream == true
            ? await StreamAsync(messages, options, model,request.StreamOptions?.IncludeUsage == true, cancellationToken)
            : await CompleteOnceAsync(messages, options, model, cancellationToken);
    }

    private async Task<IActionResult> CompleteOnceAsync(
        List<ChatMessage> messages, ChatOptions options, string model, CancellationToken ct)
    {
        try
        {
            var response = await _chatClient.GetResponseAsync(messages, options, ct);

            return Ok(new ChatCompletionResponse
            {
                Id = response.ResponseId ?? NewCompletionId(),
                Created = response.CreatedAt?.ToUnixTimeSeconds() ?? UnixNow(),
                Model = response.ModelId ?? model,
                Choices =
                [
                    new ResponseChoice
                    {
                        Index = 0,
                        Message = new ResponseMessage { Content = response.Text },
                        FinishReason = OpenAiMapper.ToFinishReason(response.FinishReason)
                    }
                ],
                Usage = OpenAiMapper.ToUsage(response.Usage)
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Chat completion failed");

            return UpstreamError();
        }
    }

    private async Task<IActionResult> StreamAsync(List<ChatMessage> messages, ChatOptions options, string model, bool includeUsage, CancellationToken ct)
    {
        var id = NewCompletionId();
        var created = UnixNow();
        var started = false;

        string? finishReason = null;
        UsageDetails? usage = null;
        var roleSent = false;

        async Task WriteRawAsync(string data)
        {
            if (!started)
            {
                started = true;
                Response.StatusCode = StatusCodes.Status200OK;
                Response.ContentType = "text/event-stream";
                Response.Headers.CacheControl = "no-cache";
                Response.Headers["X-Accel-Buffering"] = "no"; // отключить буферизацию в nginx
            }

            await Response.WriteAsync($"data: {data}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        Task WriteChunkAsync(ChunkDelta delta, string? finish = null, UsageDto? chunkUsage = null) =>
            WriteRawAsync(JsonSerializer.Serialize(new ChatCompletionChunk
            {
                Id = id,
                Created = created,
                Model = model,
                Choices = chunkUsage is null
                    ? [new ChunkChoice { Index = 0, Delta = delta, FinishReason = finish }]
                    : [],
                Usage = chunkUsage
            }));

        Task EnsureRoleSentAsync()
        {
            if (roleSent) return Task.CompletedTask;
            roleSent = true;
            return WriteChunkAsync(new ChunkDelta { Role = "assistant", Content = string.Empty });
        }

        try
        {
            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, options, ct))
            {
                if (update.FinishReason is { } fr)
                {
                    finishReason = OpenAiMapper.ToFinishReason(fr);
                }

                foreach (var content in update.Contents)
                {
                    if (content is UsageContent u)
                    {
                        usage = u.Details;
                    }
                }

                await EnsureRoleSentAsync();

                if (!string.IsNullOrEmpty(update.Text))
                {
                    await WriteChunkAsync(new ChunkDelta { Content = update.Text });
                }
            }

            await EnsureRoleSentAsync();
            await WriteChunkAsync(new ChunkDelta(), finishReason ?? "stop");

            if (includeUsage && OpenAiMapper.ToUsage(usage) is { } usageDto)
            {
                await WriteChunkAsync(new ChunkDelta(), chunkUsage: usageDto);
            }
            
            await WriteRawAsync("[DONE]");

            return new EmptyResult();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Streaming chat completion failed");

            if (!started)
            {
                return UpstreamError();
            }

            // Заголовки уже отправлены — статус изменить нельзя, сообщаем об ошибке в потоке.
            await WriteRawAsync(JsonSerializer.Serialize(UpstreamErrorBody()));
            await WriteRawAsync("[DONE]");
            return new EmptyResult();
        }
    }

    // ---------- helpers ----------

    private IActionResult InvalidRequest(string message, string? param) =>
        BadRequest(new ErrorResponse
        {
            Error = new ErrorBody
            {
                Message = message,
                Type = "invalid_request_error",
                Param = param
            }
        });

    private ObjectResult UpstreamError() => StatusCode(StatusCodes.Status502BadGateway, UpstreamErrorBody());

    // Детали исключения клиенту не отдаём — только в лог.
    private static ErrorResponse UpstreamErrorBody() => new()
    {
        Error = new ErrorBody
        {
            Message = "The upstream model provider failed to process the request.",
            Type = "server_error"
        }
    };

    private static string NewCompletionId() => $"chatcmpl-{Guid.NewGuid():N}";

    private static long UnixNow() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
