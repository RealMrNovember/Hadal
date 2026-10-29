using System.Collections.Concurrent;
using Hadal.Game.Events;
using Hadal.Game.Mailbox;
using Hadal.Game.Persistence;
using Hadal.Game.Social;
using Hadal.Game.World;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.Enums;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;

namespace Hadal.Game
{
    /// <summary>
    /// All live player sessions on this node. Transport-agnostic: the gateway hands it a
    /// connection after the schema handshake and forwards commands.
    /// </summary>
    public sealed class GameWorld
    {
        private readonly CatalogRuntime _runtime;
        private readonly IPlayerRepository _repository;
        private readonly IClock _clock;
        private readonly INetworkSerializer _serializer;
        private readonly ILogger<GameWorld> _logger;

        private readonly ConcurrentDictionary<string, PlayerSession> _sessions = new(StringComparer.Ordinal);
        private readonly SemaphoreSlim _connectGate = new(1, 1);

        /// <summary>Loaded sessions by public player id, online or resident (docs/30 §2: offline players in a battle).</summary>
        private readonly ConcurrentDictionary<string, PlayerSession> _byPublic = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, byte> _summoning = new(StringComparer.Ordinal);
        private DateTimeOffset _lastSweep;

        public GameWorld(
            GameCatalog catalog,
            IPlayerRepository repository,
            IClock clock,
            INetworkSerializer serializer,
            ILogger<GameWorld> logger)
            : this(new CatalogRuntime(catalog), repository, clock, serializer, logger)
        {
        }

        public GameWorld(
            CatalogRuntime runtime,
            IPlayerRepository repository,
            IClock clock,
            INetworkSerializer serializer,
            ILogger<GameWorld> logger)
            : this(runtime, repository, new InMemorySocialStore(), new InMemoryWorldStore(), clock, serializer, logger)
        {
        }

        public GameWorld(
            CatalogRuntime runtime,
            IPlayerRepository repository,
            ISocialStore socialStore,
            IWorldStore worldStore,
            IClock clock,
            INetworkSerializer serializer,
            ILogger<GameWorld> logger,
            IEventStore? eventStore = null,
            IMailStore? mailStore = null)
        {
            _runtime = runtime;
            _repository = repository;
            _clock = clock;
            _serializer = serializer;
            _logger = logger;
            Social = new SocialHub(socialStore, runtime, serializer, logger);
            Map = new WorldService(worldStore, runtime, serializer, Social, logger, clock);
            Map.SessionOf = id => _byPublic.TryGetValue(id, out var loaded) ? loaded : null;
            Map.Summon = id => _ = SummonAsync(id, CancellationToken.None);
            Social.NameLookup = Map.NameOf;
            Social.AvatarLookup = Map.AvatarOf;
            Events = new EventService(eventStore ?? new InMemoryEventStore(), runtime, serializer, logger)
            {
                NameLookup = Map.NameOf,
                AvatarLookup = Map.AvatarOf,
                AllianceIdOf = Social.AllianceIdOf,
                TagOf = Social.TagOf,
                AllianceInfo = Social.AllianceInfo
            };
            Mail = new MailService(mailStore ?? new InMemoryMailStore(), serializer, logger)
            {
                NameLookup = Map.NameOf,
                AllianceIdOf = Social.AllianceIdOf
            };
        }

        /// <summary>The mailbox: system, Overseer, event and alliance mails with attachments (docs/29 §2.1).</summary>
        public MailService Mail { get; }

        /// <summary>Overseer-run events: contests, sign-in rewards, rankings (docs/29).</summary>
        public EventService Events { get; }

        /// <summary>The outer world map (docs/28).</summary>
        public WorldService Map { get; }

        /// <summary>Chat, alliances and alliance help (docs/25).</summary>
        public SocialHub Social { get; }

        public int SessionCount => _sessions.Count;

        private GameCatalog _catalog => _runtime.Current.Catalog;

        public CatalogRuntime.Snapshot Catalog => _runtime.Current;

        /// <summary>Account ids (identity keys) with a live connection.</summary>
        public IReadOnlyCollection<string> OnlineIdentities =>
            _sessions.Where(s => s.Value.Connection != null).Select(s => s.Key).ToList();

        public bool IsOnline(string identityKey) =>
            _sessions.TryGetValue(identityKey, out var session) && session.Connection != null;

        /// <summary>
        /// Swaps the rules every session plays by. Sessions first settle their economy under the
        /// old rules, then pick up the new rates.
        /// </summary>
        public void ReplaceCatalog(GameCatalog catalog, int version)
        {
            var now = _clock.UtcNow;
            foreach (var session in _sessions.Values)
                session.Settle(now);

            _runtime.Replace(catalog, version);

            foreach (var session in _sessions.Values)
                session.ApplyCatalogChange(now);

            _logger.LogInformation("Catalog version {Version} is live for {Sessions} sessions.", version, _sessions.Count);
        }

        /// <summary>
        /// Adds (or with negative amounts removes) resources: live sessions update at once, offline
        /// players are loaded, changed and saved under the connect gate so a login cannot race it.
        /// False when the player does not exist.
        /// </summary>
        /// <summary>Items the Overseer may send: catalog items (chests, cards), speed-ups, heroes and troop packs.</summary>
        public bool IsGrantableItem(string id) => _catalog.TryGetItem(id, out _) || Speedups.TryParse(id, out _, out _, out _) || GiftItems.IsValid(_catalog, id)
            || VipRules.TryParseItem(id, out _) || TeleportRules.IsItem(id) || RenameRules.IsItem(id) || PvpRules.IsShield(id);

        /// <summary>
        /// Pays for a name change (RenameRules) and puts the new name on the player, live or offline. Null when done,
        /// else the reason ("RenameCooldown", "NeedRenameCard", "UnknownPlayer"). The caller stores the name itself.
        /// </summary>
        public async Task<string?> RenameAsync(string identityKey, string name, CancellationToken cancellationToken)
        {
            await _connectGate.WaitAsync(cancellationToken);
            try
            {
                var now = _clock.UtcNow;
                if (_sessions.TryGetValue(identityKey, out var session))
                    return session.Rename(name, now);

                var state = await _repository.LoadByIdentityAsync(identityKey, cancellationToken);
                if (state == null)
                    return "UnknownPlayer";
                var reason = PlayerSession.PayRename(state, now);
                if (reason != null)
                    return reason;
                state.ExtrasJson = PlayerExtras.Write(state);
                await _repository.SaveAsync(state, cancellationToken);
                return null;
            }
            finally
            {
                _connectGate.Release();
            }
        }

        /// <summary>
        /// Credits a shop pack (a purchase, or the Overseer granting one): Hadalite plus as many VIP points, live or
        /// offline. False when the player or the pack does not exist.
        /// </summary>
        public async Task<bool> SetVipPointsAsync(string identityKey, long points, CancellationToken cancellationToken)
        {
            await _connectGate.WaitAsync(cancellationToken);
            try
            {
                if (_sessions.TryGetValue(identityKey, out var session))
                {
                    session.SetVipPoints(points, _clock.UtcNow);
                    return true;
                }

                var state = await _repository.LoadByIdentityAsync(identityKey, cancellationToken);
                if (state == null)
                    return false;
                state.VipPoints = Math.Max(0, points);
                state.IsDirty = true;
                state.ExtrasJson = PlayerExtras.Write(state);
                await _repository.SaveAsync(state, cancellationToken);
                return true;
            }
            finally
            {
                _connectGate.Release();
            }
        }

        public async Task<bool> CreditPurchaseAsync(string identityKey, string packId, CancellationToken cancellationToken)
        {
            var pack = StorePacks.Find(packId);
            if (pack == null)
                return false;
            await _connectGate.WaitAsync(cancellationToken);
            try
            {
                if (_sessions.TryGetValue(identityKey, out var session))
                {
                    session.CreditPurchase(pack, _clock.UtcNow);
                    return true;
                }

                var state = await _repository.LoadByIdentityAsync(identityKey, cancellationToken);
                if (state == null)
                    return false;
                PlayerSession.ApplyPurchase(state, pack);
                state.ExtrasJson = PlayerExtras.Write(state);
                await _repository.SaveAsync(state, cancellationToken);
                return true;
            }
            finally
            {
                _connectGate.Release();
            }
        }

        public async Task<bool> GrantAsync(string identityKey, IReadOnlyDictionary<ResourceType, long> amounts, CancellationToken cancellationToken,
            bool asPackages = false, IReadOnlyDictionary<string, int>? items = null)
        {
            await _connectGate.WaitAsync(cancellationToken);
            try
            {
                if (_sessions.TryGetValue(identityKey, out var session))
                {
                    session.Grant(amounts, _clock.UtcNow, asPackages, items);
                    return true;
                }

                var state = await _repository.LoadByIdentityAsync(identityKey, cancellationToken);
                if (state == null)
                    return false;

                PlayerSession.ApplyGrant(state, amounts, asPackages, items);
                // Packages live in the extras document: write it again or the bag change is lost on save.
                state.ExtrasJson = PlayerExtras.Write(state);
                await _repository.SaveAsync(state, cancellationToken);
                return true;
            }
            finally
            {
                _connectGate.Release();
            }
        }

        /// <summary>
        /// Mails one player (by account id), online or not. False when the player does not exist. Attachments wait in the
        /// mail until the player claims them.
        /// </summary>
        public async Task<bool> SendMailAsync(string identityKey, MailDraft draft, CancellationToken cancellationToken)
        {
            var playerId = _sessions.TryGetValue(identityKey, out var session)
                ? session.PublicId
                : (await _repository.LoadByIdentityAsync(identityKey, cancellationToken))?.PlayerId.ToString("N");
            if (playerId == null)
                return false;
            await Mail.SendAsync(new[] { playerId }, draft, _clock.UtcNow, cancellationToken);
            return true;
        }

        /// <summary>Mails every player there is (an Overseer announcement); returns how many received it.</summary>
        public async Task<int> SendMailToAllAsync(MailDraft draft, CancellationToken cancellationToken)
        {
            var ids = (await _repository.ListPlayerIdsAsync(cancellationToken)).Select(id => id.ToString("N"))
                .Concat(_sessions.Values.Select(s => s.PublicId)).Distinct(StringComparer.Ordinal).ToList();
            return await Mail.SendAsync(ids, draft, _clock.UtcNow, cancellationToken);
        }

        /// <summary>Live state when the player is loaded, otherwise the stored state; null if unknown.</summary>
        public async Task<PlayerState?> DescribeAsync(string identityKey, CancellationToken cancellationToken) =>
            _sessions.TryGetValue(identityKey, out var session)
                ? session.Describe(_clock.UtcNow)
                : await _repository.LoadByIdentityAsync(identityKey, cancellationToken);

        /// <summary>Closes the player's connection (ban, forced logout). The client may reconnect unless refused.</summary>
        /// <summary>
        /// Removes a player for good (Overseer): closes their session without saving, takes their base off the map,
        /// drops them from their alliance and deletes the player row with its buildings. Returns false when the
        /// identity had no player. The account itself belongs to the gateway.
        /// </summary>
        public async Task<bool> DeletePlayerAsync(string identityKey, CancellationToken cancellationToken)
        {
            var now = _clock.UtcNow;
            Guid? playerId = null;
            if (_sessions.TryRemove(identityKey, out var session))
            {
                session.MarkDeleted();
                _byPublic.TryRemove(session.PublicId, out _);
                playerId = session.PlayerId;
                session.Connection?.Close("account removed");
            }

            var deleted = await _repository.DeleteAsync(identityKey, cancellationToken);
            playerId ??= deleted;
            if (playerId == null)
                return false;

            var publicId = playerId.Value.ToString("N");
            Social.ForgetPlayer(publicId, now);
            Mail.Forget(publicId);
            await Map.ForgetAsync(publicId, now, cancellationToken);
            _logger.LogInformation("Deleted player {PlayerId} (identity {IdentityKey}).", publicId, identityKey);
            return true;
        }

        public Task<IReadOnlyList<(string IdentityKey, Guid PlayerId, DateTimeOffset CreatedAt)>> PlayersWithoutAccountAsync(CancellationToken cancellationToken) =>
            _repository.ListWithoutAccountAsync(cancellationToken);

        public bool Kick(string identityKey, string reason)
        {
            if (!_sessions.TryGetValue(identityKey, out var session) || session.Connection == null)
                return false;

            session.Connection.Close(reason);
            return true;
        }

        public async Task<PlayerSession> ConnectAsync(string identityKey, IClientConnection connection, CancellationToken cancellationToken,
            string? displayName = null)
        {
            if (string.IsNullOrWhiteSpace(identityKey))
                throw new ArgumentException("Identity key (account id) is required.", nameof(identityKey));

            await _connectGate.WaitAsync(cancellationToken);
            try
            {
                if (!_sessions.TryGetValue(identityKey, out var session))
                {
                    var state = await _repository.LoadByIdentityAsync(identityKey, cancellationToken)
                                ?? await CreatePlayerAsync(identityKey, cancellationToken);

                    session = new PlayerSession(state, _runtime, _serializer);
                    _sessions[identityKey] = session;
                    _byPublic[session.PublicId] = session;
                }

                var now = _clock.UtcNow;
                // Battles due before this login happen first, on the server's timeline (docs/30 §2).
                Map.Settle(now);
                try
                {
                    await Mail.LoadAsync(session.PublicId, now, cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // The game goes on without the mailbox; the next login tries again.
                    _logger.LogError(exception, "Loading the mails of player {PlayerId} failed.", session.PlayerId);
                }

                session.DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Diver" : displayName;
                session.ApplyOfflineHelps(Social.TakeOfflineHelps(session.PublicId), now);

                var previous = session.Attach(connection, now);
                if (previous != null && !ReferenceEquals(previous, connection))
                {
                    _logger.LogInformation("Player {PlayerId} reconnected; closing previous connection.", session.PlayerId);
                    previous.Close("replaced by a newer connection");
                }

                Social.OnConnected(session, now);
                Map.OnConnected(session);
                Events.OnConnected(session, now);
                Mail.OnConnected(session, now);

                return session;
            }
            finally
            {
                _connectGate.Release();
            }
        }

        public void HandleCommand(PlayerSession session, CommandEnvelope envelope)
        {
            // Every battle that arrived before this command is settled before the command can change anything.
            Map.Settle(_clock.UtcNow);
            if (SocialHub.Handles(envelope.Type))
                Social.Handle(session, envelope, _clock.UtcNow);
            else if (WorldService.Handles(envelope.Type))
                Map.Handle(session, envelope, _clock.UtcNow);
            else if (EventService.Handles(envelope.Type))
                Events.Handle(session, envelope, _clock.UtcNow);
            else if (MailService.Handles(envelope.Type))
                Mail.Handle(session, envelope, _clock.UtcNow);
            else
                session.HandleCommand(envelope, _clock.UtcNow);
        }

        /// <summary>Detaches, saves and unloads the session if this connection still owns it.</summary>
        public async Task DisconnectAsync(PlayerSession session, IClientConnection connection, CancellationToken cancellationToken)
        {
            if (!session.Detach(connection, _clock.UtcNow))
                return;

            Social.OnDisconnected(session, _clock.UtcNow);
            Map.OnDisconnected(session);
            Events.OnDisconnected(session);
            Mail.OnDisconnected(session);

            await SaveAsync(session, cancellationToken);

            // Unload only if nobody reattached while we were saving, and no battle needs the player.
            if (session.Connection == null && !Map.NeedsResident(session.PublicId))
                Unload(session);
        }

        public void Tick()
        {
            var now = _clock.UtcNow;
            try
            {
                Map.Settle(now);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Settling PvP battles failed.");
            }

            foreach (var session in _sessions.Values)
            {
                try
                {
                    session.Tick(now);
                    // Event points for what the player did since the last tick (commands and timers alike).
                    Events.Record(session, session.TakeEventScores(), now);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Tick failed for player {PlayerId}.", session.PlayerId);
                }
            }

            try
            {
                Events.Tick(now);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Event housekeeping failed.");
            }

            try
            {
                Mail.Tick(now);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Mail housekeeping failed.");
            }
        }

        private void Unload(PlayerSession session)
        {
            if (_sessions.TryRemove(new KeyValuePair<string, PlayerSession>(session.IdentityKey, session)))
                _byPublic.TryRemove(new KeyValuePair<string, PlayerSession>(session.PublicId, session));
        }

        /// <summary>
        /// Loads a player's session without a connection so the battle engine can simulate them (docs/30 §2). It ticks like
        /// an online one until no battle needs it and is then saved and unloaded.
        /// </summary>
        public async Task SummonAsync(string publicId, CancellationToken cancellationToken)
        {
            if (_byPublic.ContainsKey(publicId) || !Guid.TryParseExact(publicId, "N", out var playerId) || !_summoning.TryAdd(publicId, 0))
                return;
            try
            {
                var identity = await _repository.IdentityOfAsync(playerId, cancellationToken);
                if (identity == null)
                    return;
                await _connectGate.WaitAsync(cancellationToken);
                try
                {
                    if (_sessions.TryGetValue(identity, out var loaded))
                    {
                        _byPublic[loaded.PublicId] = loaded;
                        return;
                    }

                    var state = await _repository.LoadByIdentityAsync(identity, cancellationToken);
                    if (state == null)
                        return;
                    var session = new PlayerSession(state, _runtime, _serializer);
                    _sessions[identity] = session;
                    _byPublic[session.PublicId] = session;
                    _logger.LogInformation("Player {PlayerId} loaded for a battle.", publicId);
                }
                finally
                {
                    _connectGate.Release();
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Loading player {PlayerId} for a battle failed.", publicId);
            }
            finally
            {
                _summoning.TryRemove(publicId, out _);
            }
        }

        /// <summary>Saves and unloads sessions loaded for battles that no battle needs any more.</summary>
        private async Task SweepResidentsAsync(CancellationToken cancellationToken)
        {
            foreach (var session in _sessions.Values.Where(s => s.Connection == null).ToList())
            {
                if (Map.NeedsResident(session.PublicId))
                    continue;
                await SaveAsync(session, cancellationToken);
                if (session.Connection == null && !Map.NeedsResident(session.PublicId))
                    Unload(session);
            }
        }

        public async Task SaveAllAsync(CancellationToken cancellationToken)
        {
            foreach (var session in _sessions.Values)
                await SaveAsync(session, cancellationToken);
            if (_clock.UtcNow - _lastSweep > TimeSpan.FromMinutes(1))
            {
                _lastSweep = _clock.UtcNow;
                await SweepResidentsAsync(cancellationToken);
            }
        }

        private async Task SaveAsync(PlayerSession session, CancellationToken cancellationToken)
        {
            var snapshot = session.TakeSaveSnapshot();
            if (snapshot == null)
                return;

            try
            {
                await _repository.SaveAsync(snapshot, cancellationToken);
            }
            catch (Exception exception)
            {
                session.RestoreAfterFailedSave(snapshot);
                _logger.LogError(exception, "Saving player {PlayerId} failed; will retry.", session.PlayerId);
            }
        }

        private async Task<PlayerState> CreatePlayerAsync(string identityKey, CancellationToken cancellationToken)
        {
            var state = new PlayerState
            {
                PlayerId = Guid.NewGuid(),
                IdentityKey = identityKey,
                LastSimulatedAt = _clock.UtcNow,
                UnlockedRings = _catalog.Grid.startingUnlockedRings
            };

            foreach (var pair in _catalog.StartingWallet())
                state.Wallet[pair.Key] = pair.Value;

            await _repository.CreateAsync(state, cancellationToken);

            // Every base starts around its reactor: the catalog's preferred building for each
            // ring-0 sector is pre-placed and saved straight away.
            foreach (var starter in _catalog.StarterBuildings())
            {
                var building = new PlacedBuilding
                {
                    BuildingId = "b" + Guid.NewGuid().ToString("N"),
                    DefinitionId = starter.DefinitionId,
                    Ring = starter.Ring,
                    Sector = starter.Sector,
                    Level = 1
                };
                state.Buildings.Add(building);
                state.UnsavedBuildings.Add(building);
            }

            if (state.UnsavedBuildings.Count > 0)
                await _repository.SaveAsync(state, cancellationToken);

            _logger.LogInformation("Created player {PlayerId} for identity {IdentityKey}.", state.PlayerId, identityKey);
            return state;
        }
    }
}
