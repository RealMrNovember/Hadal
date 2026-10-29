using Hadal.Game.Persistence;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hadal.Game
{
    /// <summary>
    /// Drives the world: schema init on start, one simulation tick per
    /// <see cref="SimulationTick.IntervalSeconds"/>, periodic saves, and a final save on shutdown.
    /// </summary>
    public sealed class GameTickService : BackgroundService
    {
        private readonly GameWorld _world;
        private readonly IPlayerRepository _repository;
        private readonly GameOptions _options;
        private readonly ILogger<GameTickService> _logger;

        public GameTickService(GameWorld world, IPlayerRepository repository, IOptions<GameOptions> options, ILogger<GameTickService> logger)
        {
            _world = world;
            _repository = repository;
            _options = options.Value;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await _repository.InitializeAsync(cancellationToken);
            await _world.Social.InitializeAsync(cancellationToken);
            await _world.InitializeWorldAsync(cancellationToken);
            await _world.Events.InitializeAsync(DateTimeOffset.UtcNow, cancellationToken);
            await _world.Mail.InitializeAsync(DateTimeOffset.UtcNow, cancellationToken);
            await _world.Onboarding.InitializeAsync(cancellationToken);
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(SimulationTick.IntervalSeconds));
            var lastSave = DateTime.UtcNow;

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    _world.Tick();

                    if ((DateTime.UtcNow - lastSave).TotalSeconds >= _options.SaveIntervalSeconds)
                    {
                        lastSave = DateTime.UtcNow;
                        await _world.SaveAllAsync(stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("Saving {Count} sessions before shutdown.", _world.SessionCount);
            await _world.SaveAllAsync(cancellationToken);
            await _world.Events.DrainAsync();
            await _world.Mail.DrainAsync();
        }
    }
}
