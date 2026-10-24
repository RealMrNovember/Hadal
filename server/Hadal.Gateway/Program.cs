using Hadal.Game;
using Hadal.Game.Persistence;
using Hadal.Gateway;
using HADAL.Shared.Serialization;
using Microsoft.Extensions.Options;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GameOptions>(builder.Configuration.GetSection(GameOptions.Section));
builder.Services.Configure<GatewayOptions>(builder.Configuration.GetSection(GatewayOptions.Section));
builder.Services.AddSingleton<INetworkSerializer, ProtobufNetNetworkSerializer>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton(sp => CatalogLoader.Load(sp.GetRequiredService<IOptions<GameOptions>>().Value.CatalogPath));

// No connection string = in-memory store (local runs, tests). Production always sets one.
var connectionString = builder.Configuration.GetConnectionString("Hadal");
if (string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddSingleton<IPlayerRepository, InMemoryPlayerRepository>();
}
else
{
    builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
    builder.Services.AddSingleton<IPlayerRepository, PostgresPlayerRepository>();
}

builder.Services.AddSingleton<GameWorld>();
builder.Services.AddHostedService<GameTickService>();
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

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
