var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapMcp();

app.Run();
