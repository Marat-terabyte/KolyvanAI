using Kolyvan.Gateway.Interfaces;
using Microsoft.Extensions.AI;

namespace Kolyvan.Gateway.Services;

public class BackendChatClient : DelegatingChatClient, IBackendChatClient
{
    public BackendChatClient(IChatClient chatClient) : base(chatClient)
    {
    }
}
