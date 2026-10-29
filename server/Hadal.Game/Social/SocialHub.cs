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
    public sealed partial class SocialHub
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

            /// <summary>HelpKinds: build, heal or research.</summary>
            public string Kind { get; init; } = HelpKinds.Build;

            /// <summary>The building (build), the research node (research) or empty (heal).</summary>
            public string Target { get; init; } = string.Empty;
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
        private readonly Dictionary<string, List<(string Kind, string Target, int Seconds)>> _offlineHelps = new(StringComparer.Ordinal);
        private readonly System.Threading.Channels.Channel<Func<CancellationToken, Task>> _writes = System.Threading.Channels.Channel.CreateUnbounded<Func<CancellationToken, Task>>(new UnboundedChannelOptions { SingleReader = true });
        private long _nextChatId;

        // Invitations and applications (docs/rehber 10 §2); kept in memory, they expire.
        private sealed record Invite(string AllianceId, string InviterName, DateTimeOffset At);
        private sealed record Applicant(string PlayerId, string Name, long Power, DateTimeOffset At);
        private static readonly TimeSpan InviteLife = TimeSpan.FromHours(48);
        private readonly Dictionary<string, List<Invite>> _invites = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<Applicant>> _applications = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _names = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _avatars = new(StringComparer.Ordinal);

        /// <summary>Avatars of players who are not online (the world map remembers every base's).</summary>
        public Func<string, string?>? AvatarLookup { get; set; }

        /// <summary>Names of players who are not online and not in an alliance (the world map knows every base).</summary>
        public Func<string, string?>? NameLookup { get; set; }

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
            or CommandType.LeaveAlliance or CommandType.BrowseAlliances or CommandType.KickMember or CommandType.RequestHelp or CommandType.HelpAll
            or CommandType.SetMemberRank or CommandType.TransferLeadership or CommandType.SetAllianceSettings or CommandType.InviteToAlliance
            or CommandType.AnswerInvite or CommandType.AnswerApplication or CommandType.SetAllianceInfo or CommandType.ViewAllianceLog
            or CommandType.BuyAllianceItem or CommandType.CollectAllianceGift or CommandType.DonateAllianceResearch
            or CommandType.SetResearchFocus;

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
                    var ranked = member.Rank > 0 ? member
                        : member with { Rank = member.PlayerId == alliance.Record.LeaderId ? AlliancePermissions.Leader : AlliancePermissions.Diver };
                    alliance.Members.Add(ranked);
                    _memberOf[member.PlayerId] = member.AllianceId;
                    _names[member.PlayerId] = member.Name;
                    if (ranked != member)
                        Write(ct => _store.SaveMemberAsync(ranked, ct));
                }

                foreach (var line in chat)
                {
                    Lines(line.Channel).Add(line);
                    if (!line.IsSystem && line.SenderId.Length > 0)
                        _names[line.SenderId] = line.SenderName;
                    _nextChatId = Math.Max(_nextChatId, line.Id + 1);
                }
            }

            _logger.LogInformation("Social hub: {Alliances} alliances, {Members} members, {Lines} chat lines.", alliances.Count, members.Count, chat.Count);
            await LoadDocsAsync(cancellationToken);
        }

        // --- Sessions -----------------------------------------------------------------------------

        /// <summary>Helps alliance mates gave while the player was offline.</summary>
        public List<(string Kind, string Target, int Seconds)> TakeOfflineHelps(string playerId)
        {
            lock (_gate)
            {
                if (!_offlineHelps.Remove(playerId, out var helps))
                    return new List<(string, string, int)>();
                return helps;
            }
        }

        public void OnConnected(PlayerSession session, DateTimeOffset now)
        {
            lock (_gate)
            {
                _online[session.PublicId] = session;
                _names[session.PublicId] = session.DisplayName;
                _avatars[session.PublicId] = session.AvatarId;
                var alliance = AllianceOf(session.PublicId);
                session.SetInAlliance(alliance != null, now);
                if (alliance != null)
                {
                    UpdateMember(alliance, session.PublicId, session.DisplayName, session.Power, session.BaseLevel);
                    PromoteRecruits(alliance, now);
                    PushAlliance(alliance, now); // others see them online
                }

                session.Push(new List<EntityChangeDto>
                {
                    HistoryChange(session.PublicId),
                    MyAllianceChange(session.PublicId),
                    HelpChange(session.PublicId, now),
                    InvitesChange(session.PublicId, now),
                    CoinsChange(session.PublicId, now),
                    GiftsChange(session.PublicId, now),
                    ResearchChange(session.PublicId, now)
                }, now);
            }
        }

        public void OnDisconnected(PlayerSession session, DateTimeOffset now)
        {
            if (session.IsDeleted)
                return;
            lock (_gate)
            {
                if (!_online.TryGetValue(session.PublicId, out var current) || !ReferenceEquals(current, session))
                    return;

                _online.Remove(session.PublicId);
                _avatars[session.PublicId] = session.AvatarId;
                var alliance = AllianceOf(session.PublicId);
                if (alliance != null)
                {
                    UpdateMember(alliance, session.PublicId, session.DisplayName, session.Power, session.BaseLevel, lastSeen: now);
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
                        Done(session, request!.CommandId, request.ClientSequence, RequestHelp(session, request!, now), now);
                        break;
                    case CommandType.HelpAll when TryDeserialize(envelope.Payload, out HelpAllCommand? helpAll):
                        Done(session, helpAll!.CommandId, helpAll.ClientSequence, HelpAll(session, helpAll.Kind, now), now);
                        break;
                    case CommandType.SetMemberRank when TryDeserialize(envelope.Payload, out SetMemberRankCommand? rank):
                        Done(session, rank!.CommandId, rank.ClientSequence, SetRank(session.PublicId, rank, now), now);
                        break;
                    case CommandType.TransferLeadership when TryDeserialize(envelope.Payload, out TransferLeadershipCommand? transfer):
                        Done(session, transfer!.CommandId, transfer.ClientSequence, Transfer(session.PublicId, transfer.MemberId, now), now);
                        break;
                    case CommandType.DonateAllianceResearch when TryDeserialize(envelope.Payload, out DonateAllianceResearchCommand? donate):
                        Done(session, donate!.CommandId, donate.ClientSequence, Donate(session, now), now);
                        break;
                    case CommandType.SetResearchFocus when TryDeserialize(envelope.Payload, out SetResearchFocusCommand? focus):
                        Done(session, focus!.CommandId, focus.ClientSequence, SetFocus(session.PublicId, focus, now), now);
                        break;
                    case CommandType.CollectAllianceGift when TryDeserialize(envelope.Payload, out CollectAllianceGiftCommand? collect):
                        // Collected gifts answer with their reward report; only a refusal is acknowledged here.
                        var collected = Collect(session, collect!, now);
                        if (collected != SocialRejectReason.None)
                            Done(session, collect!.CommandId, collect.ClientSequence, collected, now);
                        break;
                    case CommandType.BuyAllianceItem when TryDeserialize(envelope.Payload, out BuyAllianceItemCommand? buy):
                        // A purchase answers itself with its reward report; only a refusal is acknowledged here.
                        var bought = Buy(session, buy!, now);
                        if (bought != SocialRejectReason.None)
                            Done(session, buy!.CommandId, buy.ClientSequence, bought, now);
                        break;
                    case CommandType.ViewAllianceLog when TryDeserialize(envelope.Payload, out ViewAllianceLogCommand? viewLog):
                        Done(session, viewLog!.CommandId, viewLog.ClientSequence, ViewLog(session, viewLog, now), now);
                        break;
                    case CommandType.SetAllianceInfo when TryDeserialize(envelope.Payload, out SetAllianceInfoCommand? info):
                        Done(session, info!.CommandId, info.ClientSequence, SetInfo(session.PublicId, info, now), now);
                        break;
                    case CommandType.SetAllianceSettings when TryDeserialize(envelope.Payload, out SetAllianceSettingsCommand? settings):
                        Done(session, settings!.CommandId, settings.ClientSequence, Settings(session.PublicId, settings, now), now);
                        break;
                    case CommandType.InviteToAlliance when TryDeserialize(envelope.Payload, out InviteToAllianceCommand? invite):
                        Done(session, invite!.CommandId, invite.ClientSequence, InvitePlayer(session, invite.PlayerId, now), now);
                        break;
                    case CommandType.AnswerInvite when TryDeserialize(envelope.Payload, out AnswerInviteCommand? answer):
                        Done(session, answer!.CommandId, answer.ClientSequence, AnswerInvite(session, answer, now), now);
                        break;
                    case CommandType.AnswerApplication when TryDeserialize(envelope.Payload, out AnswerApplicationCommand? application):
                        Done(session, application!.CommandId, application.ClientSequence, AnswerApplication(session.PublicId, application, now), now);
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
            var alliance = AllianceOf(session.PublicId);
            string channel;
            if (command.Channel == (int)ChatChannel.Private)
            {
                var target = command.TargetId ?? string.Empty;
                if (target.Length == 0 || target == session.PublicId || NameOf(target).Length == 0)
                    return SocialRejectReason.UnknownPlayer;
                channel = PrivateChannel(session.PublicId, target);
            }
            else if (command.Channel == (int)ChatChannel.Alliance)
            {
                if (alliance == null)
                    return SocialRejectReason.NotInAlliance;
                channel = AllianceChannel(alliance.Record.Id);
            }
            else
            {
                channel = WorldChannel;
            }

            if (_lastChat.TryGetValue(session.PublicId, out var last) && (now - last).TotalSeconds < rules.minIntervalSeconds)
                return SocialRejectReason.TooFast;

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
            var emblem = SocialRules.IsValidEmblem(command.Emblem) ? command.Emblem : SocialRules.DefaultEmblem;
            var alliance = new Alliance
            {
                Record = new AllianceRecord("a" + Guid.NewGuid().ToString("N"), name, tag, description, session.PublicId, now, 0, emblem,
                    Language: SocialRules.IsLanguageCode(command.Language) ? command.Language : string.Empty)
            };
            _alliances[alliance.Record.Id] = alliance;
            Write(ct => _store.SaveAllianceAsync(alliance.Record, ct));
            AddMember(alliance, session.PublicId, session.DisplayName, session.Power, now, AlliancePermissions.Leader);
            ForgetPending(session.PublicId);
            session.SetInAlliance(true, now);

            session.Push(new List<EntityChangeDto> { HistoryChange(session.PublicId), HelpChange(session.PublicId, now), InvitesChange(session.PublicId, now) }, now);
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

            // An invitation opens any alliance; otherwise the join mode decides.
            if (!HasInvite(session.PublicId, alliance.Record.Id, now))
            {
                if (alliance.Record.JoinMode == (int)AllianceJoinMode.Closed)
                    return SocialRejectReason.InviteOnly;
                if (alliance.Record.JoinMode == (int)AllianceJoinMode.Approval)
                {
                    if (!_applications.TryGetValue(alliance.Record.Id, out var list))
                        _applications[alliance.Record.Id] = list = new List<Applicant>();
                    if (list.Any(a => a.PlayerId == session.PublicId))
                        return SocialRejectReason.AlreadyApplied;
                    list.Add(new Applicant(session.PublicId, session.DisplayName, session.Power, now));
                    session.Push(new List<EntityChangeDto> { InvitesChange(session.PublicId, now) }, now);
                    PushAlliance(alliance, now);
                    return SocialRejectReason.None;
                }
            }

            Admit(alliance, session.PublicId, session.DisplayName, session.Power, now);
            return SocialRejectReason.None;
        }

        /// <summary>Makes a player a recruit (R1) and tells everyone who needs to know.</summary>
        private void Admit(Alliance alliance, string playerId, string name, long power, DateTimeOffset now)
        {
            AddMember(alliance, playerId, name, power, now, AlliancePermissions.Recruit);
            ForgetPending(playerId);
            if (_online.TryGetValue(playerId, out var session))
            {
                session.SetInAlliance(true, now);
                session.Push(new List<EntityChangeDto> { HistoryChange(playerId), HelpChange(playerId, now), InvitesChange(playerId, now) }, now);
                ApplyResearchTo(session, now); // the new alliance's research bonuses
            }

            SystemMessage(alliance, "sys.joined", name, now);
            PushAlliance(alliance, now);
        }

        /// <summary>Drops every invitation and application of a player who joined somewhere.</summary>
        private void ForgetPending(string playerId)
        {
            _invites.Remove(playerId);
            foreach (var list in _applications.Values)
                list.RemoveAll(a => a.PlayerId == playerId);
        }

        private bool HasInvite(string playerId, string allianceId, DateTimeOffset now) =>
            _invites.TryGetValue(playerId, out var list) && list.Any(i => i.AllianceId == allianceId && now - i.At < InviteLife);

        private int RankOf(Alliance alliance, string playerId) =>
            alliance.Members.FirstOrDefault(m => m.PlayerId == playerId)?.Rank ?? 0;

        private void SetMember(Alliance alliance, MemberRecord member)
        {
            var index = alliance.Members.FindIndex(m => m.PlayerId == member.PlayerId);
            if (index < 0)
                return;
            alliance.Members[index] = member;
            Write(ct => _store.SaveMemberAsync(member, ct));
        }

        /// <summary>Recruits become divers by themselves after <see cref="AlliancePermissions.RecruitHours"/>.</summary>
        private void PromoteRecruits(Alliance alliance, DateTimeOffset now)
        {
            foreach (var m in alliance.Members.Where(m => m.Rank == AlliancePermissions.Recruit && (now - m.JoinedAt).TotalHours >= AlliancePermissions.RecruitHours).ToList())
                SetMember(alliance, m with { Rank = AlliancePermissions.Diver });
        }

        private SocialRejectReason SetRank(string actorId, SetMemberRankCommand command, DateTimeOffset now)
        {
            var alliance = AllianceOf(actorId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            var target = alliance.Members.FirstOrDefault(m => m.PlayerId == command.MemberId);
            if (target == null || target.PlayerId == actorId)
                return SocialRejectReason.UnknownMember;

            var actor = RankOf(alliance, actorId);
            var rank = command.Rank <= 0 ? target.Rank : command.Rank;
            var role = command.Role ?? target.Role;
            if (rank != target.Rank && !AlliancePermissions.CanSetRank(actor, target.Rank, rank))
                return SocialRejectReason.NotAllowed;
            if (role != target.Role && (!AlliancePermissions.CanSetRole(actor) || Array.IndexOf(AlliancePermissions.Roles, role) < 0))
                return SocialRejectReason.NotAllowed;
            if (rank == AlliancePermissions.Officer && target.Rank != AlliancePermissions.Officer
                && alliance.Members.Count(m => m.Rank == AlliancePermissions.Officer) >= AlliancePermissions.MaxOfficers)
                return SocialRejectReason.RankFull;

            SetMember(alliance, target with { Rank = rank, Role = rank == AlliancePermissions.Officer ? role : string.Empty });
            if (rank != target.Rank)
                SystemMessage(alliance, "sys.rank", target.Name + "|" + rank, now);
            PushAlliance(alliance, now);
            return SocialRejectReason.None;
        }

        private SocialRejectReason Transfer(string leaderId, string memberId, DateTimeOffset now)
        {
            var alliance = AllianceOf(leaderId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            if (!AlliancePermissions.CanTransfer(RankOf(alliance, leaderId)))
                return SocialRejectReason.NotLeader;
            var heir = alliance.Members.FirstOrDefault(m => m.PlayerId == memberId);
            var leader = alliance.Members.First(m => m.PlayerId == leaderId);
            if (heir == null || memberId == leaderId)
                return SocialRejectReason.UnknownMember;

            SetMember(alliance, heir with { Rank = AlliancePermissions.Leader, Role = string.Empty });
            SetMember(alliance, leader with { Rank = AlliancePermissions.Officer });
            alliance.Record = alliance.Record with { LeaderId = memberId };
            var record = alliance.Record;
            Write(ct => _store.SaveAllianceAsync(record, ct));
            SystemMessage(alliance, "sys.leader", heir.Name, now);
            PushAlliance(alliance, now);
            return SocialRejectReason.None;
        }

        private SocialRejectReason Settings(string leaderId, SetAllianceSettingsCommand command, DateTimeOffset now)
        {
            var alliance = AllianceOf(leaderId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            if (!AlliancePermissions.CanEditSettings(RankOf(alliance, leaderId)))
                return SocialRejectReason.NotLeader;
            if (!Enum.IsDefined(typeof(AllianceJoinMode), command.JoinMode))
                return SocialRejectReason.NotAllowed;

            var description = SocialRules.CleanMessage(command.Description, new HADAL.Shared.Catalog.ChatCatalogData { maxLength = 200 });

            // Name, tag and banner change only when given, with the same checks as founding.
            var rules = Catalog.Alliance;
            var name = string.IsNullOrWhiteSpace(command.Name) ? alliance.Record.Name : command.Name.Trim();
            var tag = string.IsNullOrWhiteSpace(command.Tag) ? alliance.Record.Tag : SocialRules.NormalizeTag(command.Tag);
            if (!SocialRules.IsValidName(name, rules))
                return SocialRejectReason.InvalidName;
            if (!SocialRules.IsValidTag(tag, rules))
                return SocialRejectReason.InvalidTag;
            if (_alliances.Values.Any(a => a != alliance && string.Equals(a.Record.Name, name, StringComparison.OrdinalIgnoreCase)))
                return SocialRejectReason.NameTaken;
            if (_alliances.Values.Any(a => a != alliance && string.Equals(a.Record.Tag, tag, StringComparison.OrdinalIgnoreCase)))
                return SocialRejectReason.TagTaken;
            var emblem = string.IsNullOrEmpty(command.Emblem) ? alliance.Record.Emblem : command.Emblem;
            if (!string.IsNullOrEmpty(emblem) && !SocialRules.IsValidEmblem(emblem))
                return SocialRejectReason.NotAllowed;

            alliance.Record = alliance.Record with { JoinMode = command.JoinMode, Description = description, Name = name, Tag = tag, Emblem = emblem ?? string.Empty };
            var record = alliance.Record;
            Write(ct => _store.SaveAllianceAsync(record, ct));

            // Opening up admits everyone who was waiting.
            if (command.JoinMode == (int)AllianceJoinMode.Open && _applications.Remove(alliance.Record.Id, out var waiting))
            {
                foreach (var a in waiting.Where(a => AllianceOf(a.PlayerId) == null))
                {
                    if (alliance.Members.Count >= Math.Max(1, Catalog.Alliance.maxMembers))
                        break;
                    Admit(alliance, a.PlayerId, NameOf(a.PlayerId) is { Length: > 0 } n ? n : a.Name, a.Power, now);
                }
            }

            PushAlliance(alliance, now);
            return SocialRejectReason.None;
        }

        private SocialRejectReason InvitePlayer(PlayerSession session, string playerId, DateTimeOffset now)
        {
            var alliance = AllianceOf(session.PublicId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            if (!AlliancePermissions.CanInvite(RankOf(alliance, session.PublicId)))
                return SocialRejectReason.NotAllowed;
            if (string.IsNullOrEmpty(playerId) || playerId == session.PublicId || NameOf(playerId).Length == 0)
                return SocialRejectReason.UnknownPlayer;
            if (AllianceOf(playerId) != null)
                return SocialRejectReason.AlreadyInAlliance;
            if (HasInvite(playerId, alliance.Record.Id, now))
                return SocialRejectReason.AlreadyInvited;

            if (!_invites.TryGetValue(playerId, out var list))
                _invites[playerId] = list = new List<Invite>();
            list.RemoveAll(i => i.AllianceId == alliance.Record.Id || now - i.At >= InviteLife);
            list.Add(new Invite(alliance.Record.Id, session.DisplayName, now));
            if (_online.TryGetValue(playerId, out var invitee))
                invitee.Push(new List<EntityChangeDto> { InvitesChange(playerId, now) }, now);
            return SocialRejectReason.None;
        }

        private SocialRejectReason AnswerInvite(PlayerSession session, AnswerInviteCommand command, DateTimeOffset now)
        {
            if (!HasInvite(session.PublicId, command.AllianceId, now) || !_alliances.TryGetValue(command.AllianceId, out var alliance))
                return SocialRejectReason.NoInvite;

            if (!command.Accept)
            {
                _invites[session.PublicId].RemoveAll(i => i.AllianceId == command.AllianceId);
                session.Push(new List<EntityChangeDto> { InvitesChange(session.PublicId, now) }, now);
                return SocialRejectReason.None;
            }

            if (AllianceOf(session.PublicId) != null)
                return SocialRejectReason.AlreadyInAlliance;
            if (alliance.Members.Count >= Math.Max(1, Catalog.Alliance.maxMembers))
                return SocialRejectReason.AllianceFull;
            Admit(alliance, session.PublicId, session.DisplayName, session.Power, now);
            return SocialRejectReason.None;
        }

        private SocialRejectReason AnswerApplication(string actorId, AnswerApplicationCommand command, DateTimeOffset now)
        {
            var alliance = AllianceOf(actorId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            if (!AlliancePermissions.CanApprove(RankOf(alliance, actorId)))
                return SocialRejectReason.NotAllowed;
            if (!_applications.TryGetValue(alliance.Record.Id, out var list) || list.All(a => a.PlayerId != command.PlayerId))
                return SocialRejectReason.NoApplication;

            var applicant = list.First(a => a.PlayerId == command.PlayerId);
            list.Remove(applicant);
            if (command.Accept)
            {
                if (AllianceOf(applicant.PlayerId) != null)
                    return SocialRejectReason.AlreadyInAlliance;
                if (alliance.Members.Count >= Math.Max(1, Catalog.Alliance.maxMembers))
                    return SocialRejectReason.AllianceFull;
                Admit(alliance, applicant.PlayerId, NameOf(applicant.PlayerId) is { Length: > 0 } n ? n : applicant.Name, applicant.Power, now);
            }
            else
            {
                if (_online.TryGetValue(applicant.PlayerId, out var declined))
                    declined.Push(new List<EntityChangeDto> { InvitesChange(applicant.PlayerId, now) }, now);
                PushAlliance(alliance, now);
            }

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
            {
                leaver.SetInAlliance(false, now);
                leaver.SetAllianceResearch(null, now); // the alliance's research bonuses end
            }
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
                _applications.Remove(alliance.Record.Id);
                _channels.Remove(AllianceChannel(alliance.Record.Id));
                Write(ct => _store.DeleteAllianceAsync(alliance.Record.Id, ct));
                return SocialRejectReason.None;
            }

            if (alliance.Record.LeaderId == playerId)
            {
                // The highest rank takes over; among equals the longest-standing member.
                var heir = alliance.Members.OrderByDescending(m => m.Rank).ThenBy(m => m.JoinedAt).First();
                SetMember(alliance, heir with { Rank = AlliancePermissions.Leader, Role = string.Empty });
                alliance.Record = alliance.Record with { LeaderId = heir.PlayerId };
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
            if (memberId == leaderId || alliance.Members.All(m => m.PlayerId != memberId))
                return SocialRejectReason.UnknownMember;
            if (!AlliancePermissions.CanKick(RankOf(alliance, leaderId), RankOf(alliance, memberId)))
                return SocialRejectReason.NotAllowed;
            return Leave(memberId, now, leaderId);
        }

        /// <summary>A construction (the building), the Med Bay's healing or a research (the node) asks the alliance for help.</summary>
        private SocialRejectReason RequestHelp(PlayerSession session, RequestHelpCommand command, DateTimeOffset now)
        {
            var kind = HelpKinds.Normalize(command.Kind);
            if (!HelpKinds.IsValid(kind))
                return SocialRejectReason.NothingToHelp;
            var target = kind == HelpKinds.Build ? command.BuildingEntityId ?? string.Empty
                : kind == HelpKinds.Research ? command.Target ?? string.Empty
                : string.Empty;
            var alliance = AllianceOf(session.PublicId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            if (_help.Values.Any(r => r.OwnerId == session.PublicId && r.Kind == kind && r.Target == target))
                return SocialRejectReason.AlreadyRequested;
            var job = session.HelpJob(kind, target, now);
            if (job == null)
                return SocialRejectReason.NothingToHelp;

            var request = new HelpRequest
            {
                Id = "h" + Guid.NewGuid().ToString("N"),
                AllianceId = alliance.Record.Id,
                OwnerId = session.PublicId,
                OwnerName = session.DisplayName,
                Kind = kind,
                Target = target,
                DefinitionId = job.Value.DefinitionId,
                TargetLevel = job.Value.TargetLevel,
                TotalSeconds = job.Value.TotalSeconds,
                EndsAt = job.Value.EndsAt
            };
            _help[request.Id] = request;
            PushHelp(alliance, now);
            return SocialRejectReason.None;
        }

        /// <summary>
        /// Helps every open request of the helper's mates once, or only one group (<paramref name="kind"/>: HelpKinds.Heal
        /// or HelpKinds.General). Online owners see their timers drop at once with a note of who helped; offline ones get
        /// the helps on their next login.
        /// </summary>
        private SocialRejectReason HelpAll(PlayerSession helper, string? kind, DateTimeOffset now)
        {
            var helperId = helper.PublicId;
            var alliance = AllianceOf(helperId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;

            var rules = Catalog.Alliance;
            var helped = 0;
            var received = new Dictionary<string, (int Count, int Seconds)>(StringComparer.Ordinal);
            foreach (var request in OpenRequests(alliance.Record.Id, now)
                         .Where(r => r.OwnerId != helperId && !r.HelpedBy.Contains(helperId) && HelpKinds.Matches(kind, r.Kind)).ToList())
            {
                var seconds = SocialRules.HelpSeconds(rules, request.TotalSeconds);
                if (_online.TryGetValue(request.OwnerId, out var owner))
                {
                    if (!owner.ApplyHelp(request.Kind, request.Target, seconds, now))
                    {
                        _help.Remove(request.Id);
                        continue;
                    }

                    var (count, total) = received.TryGetValue(request.OwnerId, out var r) ? r : (0, 0);
                    received[request.OwnerId] = (count + 1, total + seconds);
                }
                else
                {
                    if (!_offlineHelps.TryGetValue(request.OwnerId, out var pending))
                        _offlineHelps[request.OwnerId] = pending = new List<(string, string, int)>();
                    pending.Add((request.Kind, request.Target, seconds));
                }

                request.EndsAt = request.EndsAt.AddSeconds(-seconds);
                request.HelpedBy.Add(helperId);
                helped++;
                if (request.HelpedBy.Count >= Math.Max(1, rules.maxHelps))
                    _help.Remove(request.Id);
            }

            if (helped == 0)
                return SocialRejectReason.NothingToHelp;

            helper.RecordEvent("help", helped, now);
            AwardHelpCoins(helper, helped, now);
            foreach (var (ownerId, (count, seconds)) in received)
            {
                if (_online.TryGetValue(ownerId, out var owner))
                    owner.Push(new List<EntityChangeDto> { HelpReceivedChange(helper.DisplayName, count, seconds) }, now);
            }

            PushHelp(alliance, now);
            return SocialRejectReason.None;
        }

        private EntityChangeDto HelpReceivedChange(string helperName, int count, int seconds) => new()
        {
            EntityId = EntityIds.HelpReceived + Guid.NewGuid().ToString("N"),
            Kind = EntityChangeKind.Create,
            Payload = _serializer.Serialize(new HelpReceivedDto { HelperName = helperName, Count = count, Seconds = seconds })
        };

        /// <summary>A removed player leaves their alliance (the next rank leads) and every invitation and application.</summary>
        public void ForgetPlayer(string playerId, DateTimeOffset now)
        {
            lock (_gate)
            {
                if (AllianceOf(playerId) != null)
                    Leave(playerId, now, kickedBy: "operator");
                _online.Remove(playerId);
                ForgetPending(playerId);
                _names.Remove(playerId);
                _offlineHelps.Remove(playerId);
                _lastChat.Remove(playerId);
            }
        }

        /// <summary>The alliance tag shown next to a player (world map), empty without an alliance.</summary>
        public string TagOf(string playerId)
        {
            lock (_gate)
                return AllianceOf(playerId)?.Record.Tag ?? string.Empty;
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

        private const string PrivatePrefix = "dm:";

        /// <summary>One channel per pair of players, ids in order so both sides land on the same one.</summary>
        private static string PrivateChannel(string a, string b) =>
            string.CompareOrdinal(a, b) < 0 ? PrivatePrefix + a + ":" + b : PrivatePrefix + b + ":" + a;

        private static (string A, string B) PrivatePair(string channel)
        {
            var parts = channel.Substring(PrivatePrefix.Length).Split(':');
            return parts.Length == 2 ? (parts[0], parts[1]) : (string.Empty, string.Empty);
        }

        /// <summary>A player's current name: online session, alliance roster, chat, then the world map.</summary>
        private string NameOf(string playerId)
        {
            if (_online.TryGetValue(playerId, out var session))
                return session.DisplayName;
            if (_names.TryGetValue(playerId, out var name))
                return name;
            name = NameLookup?.Invoke(playerId) ?? string.Empty;
            if (name.Length > 0)
                _names[playerId] = name;
            return name;
        }

        private List<ChatRecord> Lines(string channel)
        {
            if (!_channels.TryGetValue(channel, out var lines))
                _channels[channel] = lines = new List<ChatRecord>();
            return lines;
        }

        private void AddMember(Alliance alliance, string playerId, string name, long power, DateTimeOffset now, int rank)
        {
            var member = new MemberRecord(playerId, alliance.Record.Id, name, power, now, rank);
            alliance.Members.Add(member);
            _memberOf[playerId] = alliance.Record.Id;
            Write(ct => _store.SaveMemberAsync(member, ct));
        }

        private void UpdateMember(Alliance alliance, string playerId, string name, long power, int level = 0, DateTimeOffset? lastSeen = null)
        {
            var index = alliance.Members.FindIndex(m => m.PlayerId == playerId);
            if (index < 0)
                return;
            var current = alliance.Members[index];
            var member = current with
            {
                Name = name, Power = power, Level = level > 0 ? level : current.Level, LastSeen = lastSeen ?? current.LastSeen
            };
            _names[playerId] = name;
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

        private void SystemMessage(Alliance alliance, string key, string argument, DateTimeOffset now)
        {
            Post(new ChatRecord(_nextChatId++, AllianceChannel(alliance.Record.Id), string.Empty, string.Empty, alliance.Record.Tag,
                key + "|" + argument, now, true), now);
            LogNotice(alliance, key, argument, now);
        }

        private IEnumerable<PlayerSession> Readers(string channel)
        {
            if (channel == WorldChannel)
                return _online.Values.ToList();
            if (channel.StartsWith(PrivatePrefix, StringComparison.Ordinal))
            {
                var (a, b) = PrivatePair(channel);
                return new[] { a, b }.Select(id => _online.TryGetValue(id, out var s) ? s : null).Where(s => s != null).Cast<PlayerSession>().ToList();
            }
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
            foreach (var (channel, privateLines) in _channels.Where(c => c.Key.StartsWith(PrivatePrefix, StringComparison.Ordinal)))
            {
                var (a, b) = PrivatePair(channel);
                if (a == playerId || b == playerId)
                    lines.AddRange(privateLines);
            }

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
                JoinMode = alliance.Record.JoinMode,
                Emblem = EmblemOf(alliance),
                Announcement = alliance.Record.Announcement ?? string.Empty,
                Language = alliance.Record.Language ?? string.Empty,
                Applicants = AlliancePermissions.CanApprove(RankOf(alliance, playerId)) && _applications.TryGetValue(alliance.Record.Id, out var waiting)
                    ? waiting.Select(a => new AllianceApplicantDto { PlayerId = a.PlayerId, Name = NameOf(a.PlayerId) is { Length: > 0 } n ? n : a.Name, Power = a.Power }).ToArray()
                    : Array.Empty<AllianceApplicantDto>(),
                Members = alliance.Members.Select(m =>
                {
                    var online = _online.TryGetValue(m.PlayerId, out var s);
                    return new AllianceMemberDto
                    {
                        PlayerId = m.PlayerId,
                        Name = online ? s!.DisplayName : m.Name,
                        IsLeader = m.PlayerId == alliance.Record.LeaderId,
                        Power = online ? s!.Power : m.Power,
                        Online = online,
                        Rank = m.Rank,
                        Role = m.Role,
                        AvatarId = AvatarOf(m.PlayerId),
                        Level = online ? s!.BaseLevel : m.Level,
                        LastSeenMs = online ? 0 : m.LastSeen?.ToUnixTimeMilliseconds() ?? 0
                    };
                }).OrderByDescending(m => m.Rank).ThenByDescending(m => m.Power).ToArray()
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
                    Power = a.Members.Sum(m => _online.TryGetValue(m.PlayerId, out var s) ? s.Power : m.Power),
                    JoinMode = a.Record.JoinMode,
                    Emblem = EmblemOf(a)
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
                    HelpedByMe = r.HelpedBy.Contains(playerId),
                    Kind = r.Kind
                }).ToArray();
            return new EntityChangeDto { EntityId = EntityIds.AllianceHelp, Kind = EntityChangeKind.Update, Payload = _serializer.Serialize(new HelpListDto { Requests = requests }) };
        }

        private EntityChangeDto InvitesChange(string playerId, DateTimeOffset now)
        {
            var invites = _invites.TryGetValue(playerId, out var list) ? list.Where(i => now - i.At < InviteLife && _alliances.ContainsKey(i.AllianceId)).ToList() : new List<Invite>();
            var dto = new AllianceInviteListDto
            {
                Invites = invites.Select(i =>
                {
                    var a = _alliances[i.AllianceId];
                    return new AllianceInviteDto
                    {
                        AllianceId = a.Record.Id,
                        Name = a.Record.Name,
                        Tag = a.Record.Tag,
                        InviterName = i.InviterName,
                        Members = a.Members.Count,
                        MaxMembers = Catalog.Alliance.maxMembers,
                        Emblem = EmblemOf(a)
                    };
                }).ToArray(),
                Applied = _applications.Where(p => p.Value.Any(a => a.PlayerId == playerId)).Select(p => p.Key).ToArray()
            };
            return new EntityChangeDto { EntityId = EntityIds.AllianceInvites, Kind = EntityChangeKind.Update, Payload = _serializer.Serialize(dto) };
        }

        private static string EmblemOf(Alliance alliance) =>
            string.IsNullOrEmpty(alliance.Record.Emblem) ? SocialRules.DefaultEmblem : alliance.Record.Emblem;

        private string AvatarOf(string playerId)
        {
            if (_online.TryGetValue(playerId, out var session))
                return session.AvatarId;
            if (_avatars.TryGetValue(playerId, out var avatar))
                return avatar;
            avatar = AvatarLookup?.Invoke(playerId) ?? string.Empty;
            _avatars[playerId] = avatar;
            return avatar;
        }

        /// <summary>The id of a player's alliance (events score for it); empty without one.</summary>
        public string AllianceIdOf(string playerId)
        {
            lock (_gate)
                return AllianceOf(playerId)?.Record.Id ?? string.Empty;
        }

        /// <summary>A player's rank in their alliance (1–5), 0 without one (territory claims).</summary>
        public int RankOf(string playerId)
        {
            lock (_gate)
                return AllianceOf(playerId) is { } alliance ? RankOf(alliance, playerId) : 0;
        }

        /// <summary>How many members an alliance has; 0 when it no longer exists (territory limit).</summary>
        public int MemberCount(string allianceId)
        {
            lock (_gate)
                return _alliances.TryGetValue(allianceId, out var a) ? a.Members.Count : 0;
        }

        /// <summary>A player's alliance mates, leader and officers first (alliance teleports); empty without one.</summary>
        public IReadOnlyList<string> AllianceMatesOf(string playerId)
        {
            lock (_gate)
            {
                var alliance = AllianceOf(playerId);
                return alliance == null
                    ? Array.Empty<string>()
                    : alliance.Members.Where(m => m.PlayerId != playerId).OrderByDescending(m => m.Rank).ThenBy(m => m.JoinedAt)
                        .Select(m => m.PlayerId).ToList();
            }
        }

        /// <summary>Tag, name and emblem of an alliance (event rankings); null when it no longer exists.</summary>
        public (string Tag, string Name, string Emblem)? AllianceInfo(string allianceId)
        {
            lock (_gate)
                return _alliances.TryGetValue(allianceId, out var a) ? (a.Record.Tag, a.Record.Name, a.Record.Emblem) : null;
        }

        /// <summary>The alliance tag and name of a player (profile cards); empty without one.</summary>
        public (string Tag, string Name) AllianceOfPlayer(string playerId)
        {
            lock (_gate)
            {
                var alliance = AllianceOf(playerId);
                return alliance == null ? (string.Empty, string.Empty) : (alliance.Record.Tag, alliance.Record.Name);
            }
        }

        private ChatMessageDto ToDto(ChatRecord line)
        {
            var dto = ToDtoBase(line);
            if (!line.IsSystem && line.SenderId.Length > 0)
                dto.SenderAvatar = AvatarOf(line.SenderId);
            if (line.Channel.StartsWith(PrivatePrefix, StringComparison.Ordinal))
            {
                var (a, b) = PrivatePair(line.Channel);
                dto.Channel = (int)ChatChannel.Private;
                dto.TargetId = a == line.SenderId ? b : a;
                dto.TargetName = NameOf(dto.TargetId);
            }

            return dto;
        }

        private static ChatMessageDto ToDtoBase(ChatRecord line) => new()
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
