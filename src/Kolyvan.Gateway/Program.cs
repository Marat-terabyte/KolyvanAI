using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Kolyvan.Gateway;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var client = new ChatClient(
            model: "anthropic/claude-haiku-5-5",
            credential: new ApiKeyCredential(""),
            options: new OpenAIClientOptions
            {
                Endpoint = new Uri("https://api.timeweb.ai/v1"),
            }
        ).AsIChatClient();

        builder.Services.AddSingleton<IChatClient>(client);

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.MapControllers();

        app.Run();
    }
}
