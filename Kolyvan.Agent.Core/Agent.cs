using Kolyvan.Agent.Core.ContextManagers;
using Kolyvan.Agent.Core.Outputs;
using Kolyvan.Agent.Core.Tools;
using Microsoft.Extensions.AI;

namespace Kolyvan.Agent.Core;

public class Agent
{
    private readonly IChatClient _chatClient;
    private readonly IContextManager _contextManager;
    private readonly IToolRepository _toolRepository;
    private readonly IAgentOutput _agentOutput;

    public Agent(IChatClient chatClient, IContextManager contextManager, IToolRepository toolRepository, IAgentOutput agentOutput)
    {
        _chatClient = chatClient;
        _contextManager = contextManager;
        _toolRepository = toolRepository;
        _agentOutput = agentOutput;
    }

    public async Task PromptAsync(string userPrompt, CancellationToken cancellationToken)
    {
        await _contextManager.AddMessageAsync(new ChatMessage(ChatRole.User, userPrompt), cancellationToken);

        IList<AITool> tools = await _toolRepository.GetToolsAsync(cancellationToken);

        ChatOptions chatOptions = new()
        {
            Tools = tools
        };

        while (true)
        {
            List<AIContent> responseContents = [];

            List<ChatMessage> messages = await _contextManager.GetChatMessagesAsync(cancellationToken);
            await foreach (ChatResponseUpdate update in _chatClient.GetStreamingResponseAsync(messages, chatOptions, cancellationToken))
            {
                foreach (AIContent content in update.Contents)
                {
                    responseContents.Add(content);

                    if (content is TextContent textContent)
                    {
                        await _agentOutput.OnAssistantTextAsync(textContent.Text, cancellationToken);
                    }
                }
            }

            ChatMessage assistantMessage = new(ChatRole.Assistant, responseContents);

            await _contextManager.AddMessageAsync(assistantMessage, cancellationToken);

            List<FunctionCallContent> toolCalls = responseContents.OfType<FunctionCallContent>().ToList();

            if (toolCalls.Count == 0)
            {
                break;
            }

            foreach (FunctionCallContent toolCall in toolCalls)
            {
                await _agentOutput.OnToolCallAsync(toolCall.Name, toolCall.Arguments, cancellationToken);

                object? result = await _toolRepository.InvokeToolAsync(toolCall.Name, toolCall.Arguments, cancellationToken);

                await _agentOutput.OnToolResultAsync(toolCall.Name, result, cancellationToken);

                ChatMessage toolMessage = new ChatMessage(ChatRole.Tool, [new FunctionResultContent(toolCall.CallId, result)]);
                await _contextManager.AddMessageAsync(toolMessage, cancellationToken);
            }
        }
    }
}
