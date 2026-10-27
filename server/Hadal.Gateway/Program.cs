using Hadal.Game;
using Hadal.Game.Events;
using Hadal.Game.Persistence;
using Hadal.Game.Social;
using Hadal.Game.World;
using Hadal.Gateway;
using Hadal.Gateway.Admin;
using Hadal.Gateway.Auth;
using HADAL.Shared.Serialization;
using Microsoft.Extensions.Options;
using Npgsql;

// `dotnet Hadal.Gateway.dll admin ...` manages Overseer accounts and exits (see AdminCli).
if (args.Length > 0 && args[0] == "admin")
    return await AdminCli.RunAsync(args[1..]);

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GameOptions>(builder.Configuration.GetSection(GameOptions.Section));
builder.Services.Configure<GatewayOptions>(builder.Configuration.GetSection(GatewayOptions.Section));
builder.Services.AddSingleton<INetworkSerializer, ProtobufNetNetworkSerializer>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton(sp => CatalogLoader.Load(sp.GetRequiredService<IOptions<GameOptions>>().Value.CatalogPath));
builder.Services.AddSingleton(sp => new CatalogRuntime(sp.GetRequiredService<HADAL.Shared.Catalog.GameCatalog>()));

// No connection string = in-memory store (local runs, tests). Production always sets one.
var connectionString = builder.Configuration.GetConnectionString("Hadal");
if (string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddSingleton<IPlayerRepository, InMemoryPlayerRepository>();
    builder.Services.AddSingleton<IAccountStore, InMemoryAccountStore>();
    builder.Services.AddSingleton<IAdminStore, InMemoryAdminStore>();
    builder.Services.AddSingleton<ISocialStore, InMemorySocialStore>();
    builder.Services.AddSingleton<IWorldStore, InMemoryWorldStore>();
    builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();
}
else
{
    builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
    builder.Services.AddSingleton<IPlayerRepository, PostgresPlayerRepository>();
    builder.Services.AddSingleton<IAccountStore, PostgresAccountStore>();
    builder.Services.AddSingleton<IAdminStore, PostgresAdminStore>();
    builder.Services.AddSingleton<ISocialStore, PostgresSocialStore>();
    builder.Services.AddSingleton<IWorldStore, PostgresWorldStore>();
    builder.Services.AddSingleton<IEventStore, PostgresEventStore>();
}

builder.Services.AddHadalAuth(builder.Configuration);
builder.Services.AddHadalAdmin(builder.Configuration);

builder.Services.AddSingleton(sp => new GameWorld(
    sp.GetRequiredService<CatalogRuntime>(), sp.GetRequiredService<IPlayerRepository>(), sp.GetRequiredService<ISocialStore>(), sp.GetRequiredService<IWorldStore>(), sp.GetRequiredService<IClock>(),
    sp.GetRequiredService<INetworkSerializer>(), sp.GetRequiredService<ILogger<GameWorld>>(), sp.GetRequiredService<IEventStore>()));
builder.Services.AddHostedService<GameTickService>();
builder.Services.AddSingleton<GatewaySessionHandler>();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });

app.MapHadalAdmin();
app.MapHadalAuth();

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
return 0;

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
