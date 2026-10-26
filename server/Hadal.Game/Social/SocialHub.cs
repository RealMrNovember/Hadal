using System.Threading.Channels;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;

namespace Hadal.Game.Social
{
    /// <summary>
    /// World and alliance chat, alliances and alliance help (docs/25_Heroes_Alliance_Chat.md). Everything
    /// lives in memory under one lock and is written through to <see cref="ISocialStore"/> in order on a
    /// background queue. Lock order: hub, then session — sessions never call the hub.
    /// </summary>
    public sealed class SocialHub
    {
        public const string WorldChannel = "world";

        private sealed class Alliance
        {
            public AllianceRecord Record { get; set; } = null!;
            public List<MemberRecord> Members { get; } = new();
        }

        private sealed class HelpRequest
        {
            public string Id { get; init; } = string.Empty;
            public string AllianceId { get; init; } = string.Empty;
            public string OwnerId { get; init; } = string.Empty;
            public string OwnerName { get; init; } = string.Empty;
            public string BuildingId { get; init; } = string.Empty;
            public string DefinitionId { get; init; } = string.Empty;
            public int TargetLevel { get; init; }
            public int TotalSeconds { get; init; }
            public DateTimeOffset EndsAt { get; set; }
            public HashSet<string> HelpedBy { get; } = new(StringComparer.Ordinal);
        }

        private readonly object _gate = new();
        private readonly ISocialStore _store;
        private readonly CatalogRuntime _runtime;
        private readonly INetworkSerializer _serializer;
        private readonly ILogger _logger;

        private readonly Dictionary<string, Alliance> _alliances = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _memberOf = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PlayerSession> _online = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<ChatRecord>> _channels = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTimeOffset> _lastChat = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HelpRequest> _help = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<(string BuildingId, int Seconds)>> _offlineHelps = new(StringComparer.Ordinal);
        private readonly System.Threading.Channels.Channel<Func<CancellationToken, Task>> _writes = System.Threading.Channels.Channel.CreateUnbounded<Func<CancellationToken, Task>>(new UnboundedChannelOptions { SingleReader = true });
        private long _nextChatId;

        public SocialHub(ISocialStore store, CatalogRuntime runtime, INetworkSerializer serializer, ILogger logger)
        {
            _store = store;
            _runtime = runtime;
            _serializer = serializer;
            _logger = logger;
            _nextChatId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
            _ = Task.Run(WriteLoopAsync);
        }

        private HADAL.Shared.Catalog.GameCatalog Catalog => _runtime.Current.Catalog;

        public static bool Handles(CommandType type) => type is CommandType.SendChat or CommandType.CreateAlliance or CommandType.JoinAlliance
            or CommandType.LeaveAlliance or CommandType.BrowseAlliances or CommandType.KickMember or CommandType.RequestHelp or CommandType.HelpAll;

        /// <summary>Loads alliances and recent chat. Call once at startup, after the schema exists.</summary>
        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await _store.InitializeAsync(cancellationToken);
            var (alliances, members) = await _store.LoadAlliancesAsync(cancellationToken);
            var chat = await _store.LoadRecentChatAsync(Math.Max(1, Catalog.Chat.history), cancellationToken);

            lock (_gate)
            {
                foreach (var record in alliances)
                    _alliances[record.Id] = new Alliance { Record = record };
                foreach (var member in members.OrderBy(m => m.JoinedAt))
                {
                    if (!_alliances.TryGetValue(member.AllianceId, out var alliance))
                        continue;
                    alliance.Members.Add(member);
                    _memberOf[member.PlayerId] = member.AllianceId;
                }

                foreach (var line in chat)
                {
                    Lines(line.Channel).Add(line);
                    _nextChatId = Math.Max(_nextChatId, line.Id + 1);
                }
            }

            _logger.LogInformation("Social hub: {Alliances} alliances, {Members} members, {Lines} chat lines.", alliances.Count, members.Count, chat.Count);
        }

        // --- Sessions -----------------------------------------------------------------------------

        /// <summary>Helps alliance mates gave while the player was offline.</summary>
        public List<(string BuildingId, int Seconds)> TakeOfflineHelps(string playerId)
        {
            lock (_gate)
            {
                if (!_offlineHelps.Remove(playerId, out var helps))
                    return new List<(string, int)>();
                return helps;
            }
        }

        public void OnConnected(PlayerSession session, DateTimeOffset now)
        {
            lock (_gate)
            {
                _online[session.PublicId] = session;
                var alliance = AllianceOf(session.PublicId);
                session.SetInAlliance(alliance != null, now);
                if (alliance != null)
                {
                    UpdateMember(alliance, session.PublicId, session.DisplayName, session.Power);
                    PushAlliance(alliance, now); // others see them online
                }

                session.Push(new List<EntityChangeDto>
                {
                    HistoryChange(session.PublicId),
                    MyAllianceChange(session.PublicId),
                    HelpChange(session.PublicId, now)
                }, now);
            }
        }

        public void OnDisconnected(PlayerSession session, DateTimeOffset now)
        {
            lock (_gate)
            {
                if (!_online.TryGetValue(session.PublicId, out var current) || !ReferenceEquals(current, session))
                    return;

                _online.Remove(session.PublicId);
                var alliance = AllianceOf(session.PublicId);
                if (alliance != null)
                {
                    UpdateMember(alliance, session.PublicId, session.DisplayName, session.Power);
                    PushAlliance(alliance, now);
                }
            }
        }

        // --- Commands -----------------------------------------------------------------------------

        public void Handle(PlayerSession session, CommandEnvelope envelope, DateTimeOffset now)
        {
            lock (_gate)
            {
                switch (envelope.Type)
                {
                    case CommandType.SendChat when TryDeserialize(envelope.Payload, out SendChatCommand? chat):
                        Done(session, chat!.CommandId, chat.ClientSequence, SendChat(session, chat, now), now);
                        break;
                    case CommandType.CreateAlliance when TryDeserialize(envelope.Payload, out CreateAllianceCommand? create):
                        Done(session, create!.CommandId, create.ClientSequence, Create(session, create, now), now);
                        break;
                    case CommandType.JoinAlliance when TryDeserialize(envelope.Payload, out JoinAllianceCommand? join):
                        Done(session, join!.CommandId, join.ClientSequence, Join(session, join.AllianceId, now), now);
                        break;
                    case CommandType.LeaveAlliance when TryDeserialize(envelope.Payload, out LeaveAllianceCommand? leave):
                        Done(session, leave!.CommandId, leave.ClientSequence, Leave(session.PublicId, now, kickedBy: null), now);
                        break;
                    case CommandType.BrowseAlliances when TryDeserialize(envelope.Payload, out BrowseAlliancesCommand? browse):
                        session.Acknowledge(browse!.CommandId, browse.ClientSequence, null, now, new List<EntityChangeDto> { ListChange(browse.Query) });
                        break;
                    case CommandType.KickMember when TryDeserialize(envelope.Payload, out KickMemberCommand? kick):
                        Done(session, kick!.CommandId, kick.ClientSequence, Kick(session.PublicId, kick.MemberId, now), now);
                        break;
                    case CommandType.RequestHelp when TryDeserialize(envelope.Payload, out RequestHelpCommand? request):
                        Done(session, request!.CommandId, request.ClientSequence, RequestHelp(session, request.BuildingEntityId, now), now);
                        break;
                    case CommandType.HelpAll when TryDeserialize(envelope.Payload, out HelpAllCommand? helpAll):
                        Done(session, helpAll!.CommandId, helpAll.ClientSequence, HelpAll(session.PublicId, now), now);
                        break;
                    default:
                        session.Acknowledge(envelope.CommandId, envelope.ClientSequence, "MalformedCommand", now);
                        break;
                }
            }
        }

        private static void Done(PlayerSession session, string commandId, ulong sequence, SocialRejectReason verdict, DateTimeOffset now) =>
            session.Acknowledge(commandId, sequence, verdict == SocialRejectReason.None ? null : verdict.ToString(), now);

        private SocialRejectReason SendChat(PlayerSession session, SendChatCommand command, DateTimeOffset now)
        {
            var rules = Catalog.Chat;
            var text = SocialRules.CleanMessage(command.Text, rules);
            if (text.Length == 0)
                return SocialRejectReason.EmptyMessage;
            if (_lastChat.TryGetValue(session.PublicId, out var last) && (now - last).TotalSeconds < rules.minIntervalSeconds)
                return SocialRejectReason.TooFast;

            var alliance = AllianceOf(session.PublicId);
            string channel;
            if (command.Channel == (int)ChatChannel.Alliance)
            {
                if (alliance == null)
                    return SocialRejectReason.NotInAlliance;
                channel = AllianceChannel(alliance.Record.Id);
            }
            else
            {
                channel = WorldChannel;
            }

            _lastChat[session.PublicId] = now;
            session.RecordEvent("chat", 1, now);
            Post(new ChatRecord(_nextChatId++, channel, session.PublicId, session.DisplayName, alliance?.Record.Tag ?? string.Empty, text, now, false), now);
            return SocialRejectReason.None;
        }

        private SocialRejectReason Create(PlayerSession session, CreateAllianceCommand command, DateTimeOffset now)
        {
            var rules = Catalog.Alliance;
            if (AllianceOf(session.PublicId) != null)
                return SocialRejectReason.AlreadyInAlliance;

            var name = (command.Name ?? string.Empty).Trim();
            var tag = SocialRules.NormalizeTag(command.Tag);
            if (!SocialRules.IsValidName(name, rules))
                return SocialRejectReason.InvalidName;
            if (!SocialRules.IsValidTag(tag, rules))
                return SocialRejectReason.InvalidTag;
            if (_alliances.Values.Any(a => string.Equals(a.Record.Name, name, StringComparison.OrdinalIgnoreCase)))
                return SocialRejectReason.NameTaken;
            if (_alliances.Values.Any(a => string.Equals(a.Record.Tag, tag, StringComparison.OrdinalIgnoreCase)))
                return SocialRejectReason.TagTaken;
            if (!session.TryCharge(Catalog.AllianceCreateCost, now))
                return SocialRejectReason.InsufficientResources;

            var description = SocialRules.CleanMessage(command.Description, new HADAL.Shared.Catalog.ChatCatalogData { maxLength = 200 });
            var alliance = new Alliance
            {
                Record = new AllianceRecord("a" + Guid.NewGuid().ToString("N"), name, tag, description, session.PublicId, now)
            };
            _alliances[alliance.Record.Id] = alliance;
            Write(ct => _store.SaveAllianceAsync(alliance.Record, ct));
            AddMember(alliance, session.PublicId, session.DisplayName, session.Power, now);
            session.SetInAlliance(true, now);

            session.Push(new List<EntityChangeDto> { HistoryChange(session.PublicId), HelpChange(session.PublicId, now) }, now);
            PushAlliance(alliance, now);
            return SocialRejectReason.None;
        }

        private SocialRejectReason Join(PlayerSession session, string allianceId, DateTimeOffset now)
        {
            if (AllianceOf(session.PublicId) != null)
                return SocialRejectReason.AlreadyInAlliance;
            if (!_alliances.TryGetValue(allianceId ?? string.Empty, out var alliance))
                return SocialRejectReason.UnknownAlliance;
            if (alliance.Members.Count >= Math.Max(1, Catalog.Alliance.maxMembers))
                return SocialRejectReason.AllianceFull;

            AddMember(alliance, session.PublicId, session.DisplayName, session.Power, now);
            session.SetInAlliance(true, now);
            SystemMessage(alliance, "sys.joined", session.DisplayName, now);
            session.Push(new List<EntityChangeDto> { HistoryChange(session.PublicId), HelpChange(session.PublicId, now) }, now);
            PushAlliance(alliance, now);
            return SocialRejectReason.None;
        }

        /// <summary>Leaving (or being kicked). A leaving leader hands over to the longest-standing member; the last one out disbands.</summary>
        private SocialRejectReason Leave(string playerId, DateTimeOffset now, string? kickedBy)
        {
            var alliance = AllianceOf(playerId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;

            var member = alliance.Members.First(m => m.PlayerId == playerId);
            alliance.Members.Remove(member);
            _memberOf.Remove(playerId);
            Write(ct => _store.DeleteMemberAsync(playerId, ct));
            foreach (var request in _help.Values.Where(r => r.OwnerId == playerId).ToList())
                _help.Remove(request.Id);

            if (_online.TryGetValue(playerId, out var leaver))
                leaver.SetInAlliance(false, now);
            if (leaver != null)
                leaver.Push(new List<EntityChangeDto>
                {
                    new() { EntityId = EntityIds.MyAlliance, Kind = EntityChangeKind.Delete },
                    HistoryChange(playerId),
                    HelpChange(playerId, now)
                }, now);

            if (alliance.Members.Count == 0)
            {
                _alliances.Remove(alliance.Record.Id);
                _channels.Remove(AllianceChannel(alliance.Record.Id));
                Write(ct => _store.DeleteAllianceAsync(alliance.Record.Id, ct));
                return SocialRejectReason.None;
            }

            if (alliance.Record.LeaderId == playerId)
            {
                alliance.Record = alliance.Record with { LeaderId = alliance.Members.OrderBy(m => m.JoinedAt).First().PlayerId };
                var record = alliance.Record;
                Write(ct => _store.SaveAllianceAsync(record, ct));
            }

            SystemMessage(alliance, kickedBy == null ? "sys.left" : "sys.kicked", member.Name, now);
            PushAlliance(alliance, now);
            PushHelp(alliance, now);
            return SocialRejectReason.None;
        }

        private SocialRejectReason Kick(string leaderId, string memberId, DateTimeOffset now)
        {
            var alliance = AllianceOf(leaderId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            if (alliance.Record.LeaderId != leaderId)
                return SocialRejectReason.NotLeader;
            if (memberId == leaderId || alliance.Members.All(m => m.PlayerId != memberId))
                return SocialRejectReason.UnknownMember;
            return Leave(memberId, now, leaderId);
        }

        private SocialRejectReason RequestHelp(PlayerSession session, string buildingId, DateTimeOffset now)
        {
            var alliance = AllianceOf(session.PublicId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            if (_help.Values.Any(r => r.OwnerId == session.PublicId && r.BuildingId == buildingId))
                return SocialRejectReason.AlreadyRequested;
            var construction = session.Construction(buildingId, now);
            if (construction == null)
                return SocialRejectReason.NothingToHelp;

            var request = new HelpRequest
            {
                Id = "h" + Guid.NewGuid().ToString("N"),
                AllianceId = alliance.Record.Id,
                OwnerId = session.PublicId,
                OwnerName = session.DisplayName,
                BuildingId = buildingId,
                DefinitionId = construction.Value.DefinitionId,
                TargetLevel = construction.Value.TargetLevel,
                TotalSeconds = construction.Value.TotalSeconds,
                EndsAt = construction.Value.EndsAt
            };
            _help[request.Id] = request;
            PushHelp(alliance, now);
            return SocialRejectReason.None;
        }

        private SocialRejectReason HelpAll(string helperId, DateTimeOffset now)
        {
            var alliance = AllianceOf(helperId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;

            var rules = Catalog.Alliance;
            var helped = 0;
            foreach (var request in OpenRequests(alliance.Record.Id, now).Where(r => r.OwnerId != helperId && !r.HelpedBy.Contains(helperId)).ToList())
            {
                var seconds = SocialRules.HelpSeconds(rules, request.TotalSeconds);
                if (_online.TryGetValue(request.OwnerId, out var owner))
                {
                    if (!owner.ApplyHelp(request.BuildingId, seconds, now))
                    {
                        _help.Remove(request.Id);
                        continue;
                    }
                }
                else
                {
                    if (!_offlineHelps.TryGetValue(request.OwnerId, out var pending))
                        _offlineHelps[request.OwnerId] = pending = new List<(string, int)>();
                    pending.Add((request.BuildingId, seconds));
                }

                request.EndsAt = request.EndsAt.AddSeconds(-seconds);
                request.HelpedBy.Add(helperId);
                helped++;
                if (request.HelpedBy.Count >= Math.Max(1, rules.maxHelps))
                    _help.Remove(request.Id);
            }

            if (helped == 0)
                return SocialRejectReason.NothingToHelp;

            if (_online.TryGetValue(helperId, out var helper))
                helper.RecordEvent("help", helped, now);

            PushHelp(alliance, now);
            return SocialRejectReason.None;
        }

        // --- Moderation (Overseer) ----------------------------------------------------------------

        /// <summary>Recent lines of a channel ("world" or "alliance:&lt;id&gt;"), oldest first.</summary>
        public List<ChatRecord> RecentChat(string channel)
        {
            lock (_gate)
                return _channels.TryGetValue(channel, out var lines) ? lines.ToList() : new List<ChatRecord>();
        }

        /// <summary>Removes a line everywhere; online readers get the cleaned history.</summary>
        public bool DeleteChat(long messageId, DateTimeOffset now)
        {
            lock (_gate)
            {
                var channel = _channels.FirstOrDefault(c => c.Value.Any(l => l.Id == messageId));
                if (channel.Value == null)
                    return false;

                channel.Value.RemoveAll(l => l.Id == messageId);
                Write(ct => _store.DeleteChatAsync(messageId, ct));
                foreach (var reader in Readers(channel.Key))
                    reader.Push(new List<EntityChangeDto> { HistoryChange(reader.PublicId) }, now);
                return true;
            }
        }

        // --- Internals ----------------------------------------------------------------------------

        private Alliance? AllianceOf(string playerId) =>
            _memberOf.TryGetValue(playerId, out var id) && _alliances.TryGetValue(id, out var alliance) ? alliance : null;

        private static string AllianceChannel(string allianceId) => "alliance:" + allianceId;

        private List<ChatRecord> Lines(string channel)
        {
            if (!_channels.TryGetValue(channel, out var lines))
                _channels[channel] = lines = new List<ChatRecord>();
            return lines;
        }

        private void AddMember(Alliance alliance, string playerId, string name, long power, DateTimeOffset now)
        {
            var member = new MemberRecord(playerId, alliance.Record.Id, name, power, now);
            alliance.Members.Add(member);
            _memberOf[playerId] = alliance.Record.Id;
            Write(ct => _store.SaveMemberAsync(member, ct));
        }

        private void UpdateMember(Alliance alliance, string playerId, string name, long power)
        {
            var index = alliance.Members.FindIndex(m => m.PlayerId == playerId);
            if (index < 0)
                return;
            var member = alliance.Members[index] with { Name = name, Power = power };
            alliance.Members[index] = member;
            Write(ct => _store.SaveMemberAsync(member, ct));
        }

        private void Post(ChatRecord line, DateTimeOffset now)
        {
            var lines = Lines(line.Channel);
            lines.Add(line);
            var keep = Math.Max(1, Catalog.Chat.history);
            if (lines.Count > keep)
                lines.RemoveRange(0, lines.Count - keep);
            Write(ct => _store.AddChatAsync(line, ct));

            var change = new EntityChangeDto
            {
                EntityId = EntityIds.ChatMessage + line.Id,
                Kind = EntityChangeKind.Create,
                Payload = _serializer.Serialize(ToDto(line))
            };
            foreach (var reader in Readers(line.Channel))
                reader.Push(new List<EntityChangeDto> { change }, now);
        }

        private void SystemMessage(Alliance alliance, string key, string argument, DateTimeOffset now) =>
            Post(new ChatRecord(_nextChatId++, AllianceChannel(alliance.Record.Id), string.Empty, string.Empty, alliance.Record.Tag,
                key + "|" + argument, now, true), now);

        private IEnumerable<PlayerSession> Readers(string channel)
        {
            if (channel == WorldChannel)
                return _online.Values.ToList();
            var allianceId = channel.Substring("alliance:".Length);
            return _alliances.TryGetValue(allianceId, out var alliance)
                ? alliance.Members.Select(m => _online.TryGetValue(m.PlayerId, out var s) ? s : null).Where(s => s != null).Cast<PlayerSession>().ToList()
                : Enumerable.Empty<PlayerSession>();
        }

        private IEnumerable<HelpRequest> OpenRequests(string allianceId, DateTimeOffset now)
        {
            foreach (var expired in _help.Values.Where(r => r.EndsAt <= now).ToList())
                _help.Remove(expired.Id);
            return _help.Values.Where(r => r.AllianceId == allianceId).OrderBy(r => r.EndsAt).ToList();
        }

        private void PushAlliance(Alliance alliance, DateTimeOffset now)
        {
            foreach (var member in alliance.Members)
            {
                if (_online.TryGetValue(member.PlayerId, out var session))
                    session.Push(new List<EntityChangeDto> { MyAllianceChange(member.PlayerId) }, now);
            }
        }

        private void PushHelp(Alliance alliance, DateTimeOffset now)
        {
            foreach (var member in alliance.Members)
            {
                if (_online.TryGetValue(member.PlayerId, out var session))
                    session.Push(new List<EntityChangeDto> { HelpChange(member.PlayerId, now) }, now);
            }
        }

        private EntityChangeDto HistoryChange(string playerId)
        {
            var lines = new List<ChatRecord>(Lines(WorldChannel));
            var alliance = AllianceOf(playerId);
            if (alliance != null)
                lines.AddRange(Lines(AllianceChannel(alliance.Record.Id)));

            return new EntityChangeDto
            {
                EntityId = EntityIds.ChatHistory,
                Kind = EntityChangeKind.Update,
                Payload = _serializer.Serialize(new ChatHistoryDto { Messages = lines.OrderBy(l => l.Id).Select(ToDto).ToArray() })
            };
        }

        private EntityChangeDto MyAllianceChange(string playerId)
        {
            var alliance = AllianceOf(playerId);
            if (alliance == null)
                return new EntityChangeDto { EntityId = EntityIds.MyAlliance, Kind = EntityChangeKind.Delete };

            var dto = new AllianceDto
            {
                Id = alliance.Record.Id,
                Name = alliance.Record.Name,
                Tag = alliance.Record.Tag,
                Description = alliance.Record.Description,
                LeaderId = alliance.Record.LeaderId,
                MaxMembers = Catalog.Alliance.maxMembers,
                Members = alliance.Members.Select(m =>
                {
                    var online = _online.TryGetValue(m.PlayerId, out var s);
                    return new AllianceMemberDto
                    {
                        PlayerId = m.PlayerId,
                        Name = online ? s!.DisplayName : m.Name,
                        IsLeader = m.PlayerId == alliance.Record.LeaderId,
                        Power = online ? s!.Power : m.Power,
                        Online = online
                    };
                }).OrderByDescending(m => m.IsLeader).ThenByDescending(m => m.Power).ToArray()
            };
            return new EntityChangeDto { EntityId = EntityIds.MyAlliance, Kind = EntityChangeKind.Update, Payload = _serializer.Serialize(dto) };
        }

        private EntityChangeDto ListChange(string? query)
        {
            var q = (query ?? string.Empty).Trim();
            var list = _alliances.Values
                .Where(a => q.Length == 0 || a.Record.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || a.Record.Tag.Contains(q, StringComparison.OrdinalIgnoreCase))
                .Select(a => new AllianceSummaryDto
                {
                    Id = a.Record.Id,
                    Name = a.Record.Name,
                    Tag = a.Record.Tag,
                    Description = a.Record.Description,
                    Members = a.Members.Count,
                    MaxMembers = Catalog.Alliance.maxMembers,
                    LeaderName = a.Members.FirstOrDefault(m => m.PlayerId == a.Record.LeaderId)?.Name ?? string.Empty,
                    Power = a.Members.Sum(m => _online.TryGetValue(m.PlayerId, out var s) ? s.Power : m.Power)
                })
                .OrderByDescending(a => a.Power)
                .Take(30)
                .ToArray();
            return new EntityChangeDto { EntityId = EntityIds.AllianceList, Kind = EntityChangeKind.Update, Payload = _serializer.Serialize(new AllianceListDto { Alliances = list }) };
        }

        private EntityChangeDto HelpChange(string playerId, DateTimeOffset now)
        {
            var alliance = AllianceOf(playerId);
            var requests = alliance == null
                ? Array.Empty<HelpRequestDto>()
                : OpenRequests(alliance.Record.Id, now).Where(r => r.OwnerId != playerId).Select(r => new HelpRequestDto
                {
                    RequestId = r.Id,
                    OwnerId = r.OwnerId,
                    OwnerName = r.OwnerName,
                    BuildingDefinitionId = r.DefinitionId,
                    TargetLevel = r.TargetLevel,
                    Helps = r.HelpedBy.Count,
                    MaxHelps = Catalog.Alliance.maxHelps,
                    HelpedByMe = r.HelpedBy.Contains(playerId)
                }).ToArray();
            return new EntityChangeDto { EntityId = EntityIds.AllianceHelp, Kind = EntityChangeKind.Update, Payload = _serializer.Serialize(new HelpListDto { Requests = requests }) };
        }

        private static ChatMessageDto ToDto(ChatRecord line) => new()
        {
            Id = line.Id,
            Channel = line.Channel == WorldChannel ? (int)ChatChannel.World : (int)ChatChannel.Alliance,
            SenderId = line.SenderId,
            SenderName = line.SenderName,
            AllianceTag = line.AllianceTag,
            Text = line.Text,
            SentAtUnixMs = line.SentAt.ToUnixTimeMilliseconds(),
            IsSystem = line.IsSystem
        };

        private void Write(Func<CancellationToken, Task> write) => _writes.Writer.TryWrite(write);

        private async Task WriteLoopAsync()
        {
            await foreach (var write in _writes.Reader.ReadAllAsync())
            {
                try
                {
                    await write(CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Social store write failed.");
                }
            }
        }

        private bool TryDeserialize<T>(byte[] payload, out T? value) where T : class
        {
            value = null;
            return payload is { Length: > 0 } && _serializer.TryDeserialize(payload, out value) && value != null;
        }
    }
}
