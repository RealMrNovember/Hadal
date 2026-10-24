using Hadal.Gateway;
using HADAL.Shared.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<INetworkSerializer, ProtobufNetNetworkSerializer>();
builder.Services.AddSingleton<GatewaySessionHandler>();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });

// Public health — no internal data (HADAL_SERVER_INFRASTRUCTURE.md §3).
app.MapGet("/health", () => Results.Ok(new { status = "ok", schema = SchemaVersion.Current }));

// Gameplay wire: Protobuf binary frames only. nginx/cloudflared terminates TLS.
app.Map("/gateway", async (HttpContext context, GatewaySessionHandler handler) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await handler.RunAsync(socket, context.RequestAborted);
});

app.Run();
