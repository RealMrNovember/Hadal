using System.Text.Json;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Social
{
    /// <summary>
    /// Alliance research (docs/31 step 6, AllianceResearchRules): officers pick the focus, members donate its resource
    /// for research points; every level is a bonus for every member through their modifiers, live for those online and
    /// on login for the rest. Kept as the "a:&lt;id&gt;:research" alliance document; donations also count for the
    /// alliance's donation ranking.
    /// </summary>
    public sealed partial class SocialHub
    {
        private sealed class ResearchTree
        {
            public string Focus { get; set; } = string.Empty;
            public Dictionary<string, int> Levels { get; set; } = new(StringComparer.Ordinal);
            public Dictionary<string, long> Progress { get; set; } = new(StringComparer.Ordinal);

            /// <summary>Research points each member donated in total.</summary>
            public Dictionary<string, long> Donated { get; set; } = new(StringComparer.Ordinal);
        }

        private readonly Dictionary<string, ResearchTree> _research = new(StringComparer.Ordinal);

        private static string ResearchKey(string allianceId) => $"a:{allianceId}:research";

        private ResearchTree TreeOf(string allianceId) =>
            _research.TryGetValue(allianceId, out var tree) ? tree : _research[allianceId] = new ResearchTree();

        private void SaveTree(string allianceId, ResearchTree tree)
        {
            var json = JsonSerializer.Serialize(tree);
            Write(ct => _store.SaveDocAsync(ResearchKey(allianceId), json, ct));
        }

        private static string FocusOf(ResearchTree tree) =>
            AllianceResearchRules.Find(tree.Focus) != null ? tree.Focus : AllianceResearchRules.Nodes[0].Id;

        private SocialRejectReason SetFocus(string playerId, SetResearchFocusCommand command, DateTimeOffset now)
        {
            var alliance = AllianceOf(playerId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            if (!AlliancePermissions.CanSetResearchFocus(RankOf(alliance, playerId)))
                return SocialRejectReason.NotAllowed;
            var node = AllianceResearchRules.Find(command.NodeId);
            var tree = TreeOf(alliance.Record.Id);
            if (node == null || (tree.Levels.TryGetValue(node.Id, out var level) ? level : 0) >= AllianceResearchRules.MaxLevel)
                return SocialRejectReason.NotAllowed;
            tree.Focus = node.Id;
            SaveTree(alliance.Record.Id, tree);
            PushResearch(alliance, now);
            return SocialRejectReason.None;
        }

        private SocialRejectReason Donate(PlayerSession session, DateTimeOffset now)
        {
            var alliance = AllianceOf(session.PublicId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            var tree = TreeOf(alliance.Record.Id);
            var node = AllianceResearchRules.Find(FocusOf(tree))!;
            var level = tree.Levels.TryGetValue(node.Id, out var l) ? l : 0;
            if (level >= AllianceResearchRules.MaxLevel)
                return SocialRejectReason.NotAllowed;
            var purse = PurseOf(session.PublicId, now);
            if (purse.DonationsToday >= AllianceResearchRules.DonationsPerDay)
                return SocialRejectReason.DonationLimit;
            var cost = new List<KeyValuePair<ResourceType, long>> { new(node.Resource, AllianceResearchRules.DonationCost(level)) };
            if (!session.TryCharge(cost, now, "alliance_research"))
                return SocialRejectReason.InsufficientResources;

            purse.DonationsToday++;
            SavePurse(session.PublicId, purse);
            tree.Donated[session.PublicId] = (tree.Donated.TryGetValue(session.PublicId, out var d) ? d : 0) + AllianceResearchRules.PointsPerDonation;
            var progress = (tree.Progress.TryGetValue(node.Id, out var p) ? p : 0) + AllianceResearchRules.PointsPerDonation;
            var levelled = false;
            while (level < AllianceResearchRules.MaxLevel && progress >= AllianceResearchRules.PointsToNext(level))
            {
                progress -= AllianceResearchRules.PointsToNext(level);
                level++;
                levelled = true;
                LogEvent(alliance.Record.Id, "research", new[] { session.DisplayName, node.Id, level.ToString() }, now);
            }

            tree.Levels[node.Id] = level;
            tree.Progress[node.Id] = level >= AllianceResearchRules.MaxLevel ? 0 : progress;
            SaveTree(alliance.Record.Id, tree);
            if (levelled)
                ApplyResearch(alliance, now);
            PushResearch(alliance, now);
            return SocialRejectReason.None;
        }

        /// <summary>Gives every online member the alliance's research bonuses. Caller holds the lock.</summary>
        private void ApplyResearch(Alliance alliance, DateTimeOffset now)
        {
            var levels = new Dictionary<string, int>(TreeOf(alliance.Record.Id).Levels, StringComparer.Ordinal);
            foreach (var member in alliance.Members)
            {
                if (_online.TryGetValue(member.PlayerId, out var session))
                    session.SetAllianceResearch(levels, now);
            }
        }

        /// <summary>A player's bonuses follow their alliance (login, joining, leaving). Caller holds the lock.</summary>
        private void ApplyResearchTo(PlayerSession session, DateTimeOffset now)
        {
            var alliance = AllianceOf(session.PublicId);
            session.SetAllianceResearch(alliance == null
                ? null
                : new Dictionary<string, int>(TreeOf(alliance.Record.Id).Levels, StringComparer.Ordinal), now);
            session.Push(new List<EntityChangeDto> { ResearchChange(session.PublicId, now) }, now);
        }

        private void PushResearch(Alliance alliance, DateTimeOffset now)
        {
            foreach (var member in alliance.Members)
            {
                if (_online.TryGetValue(member.PlayerId, out var session))
                    session.Push(new List<EntityChangeDto> { ResearchChange(member.PlayerId, now) }, now);
            }
        }

        private EntityChangeDto ResearchChange(string playerId, DateTimeOffset now)
        {
            var alliance = AllianceOf(playerId);
            var tree = alliance == null ? new ResearchTree() : TreeOf(alliance.Record.Id);
            return new EntityChangeDto
            {
                EntityId = EntityIds.AllianceResearch,
                Kind = EntityChangeKind.Update,
                Payload = _serializer.Serialize(new AllianceResearchDto
                {
                    Focus = FocusOf(tree),
                    Nodes = AllianceResearchRules.Nodes.Select(n =>
                    {
                        var level = tree.Levels.TryGetValue(n.Id, out var l) ? l : 0;
                        return new AllianceResearchNodeDto
                        {
                            Id = n.Id, Level = level, Progress = tree.Progress.TryGetValue(n.Id, out var p) ? p : 0,
                            Needed = AllianceResearchRules.PointsToNext(level)
                        };
                    }).ToArray(),
                    DonationsLeft = Math.Max(0, AllianceResearchRules.DonationsPerDay - PurseOf(playerId, now).DonationsToday),
                    MyDonated = tree.Donated.TryGetValue(playerId, out var mine) ? mine : 0
                })
            };
        }

        private void LoadResearch(IEnumerable<(string Key, string Json)> docs)
        {
            foreach (var (key, json) in docs)
            {
                if (!key.StartsWith("a:", StringComparison.Ordinal) || !key.EndsWith(":research", StringComparison.Ordinal))
                    continue;
                try
                {
                    _research[key.Substring(2, key.Length - 2 - ":research".Length)] = JsonSerializer.Deserialize<ResearchTree>(json) ?? new ResearchTree();
                }
                catch (JsonException)
                {
                }
            }
        }

        /// <summary>An alliance's research levels (tests and the Overseer).</summary>
        internal Dictionary<string, int> ResearchLevelsOf(string allianceId)
        {
            lock (_gate)
                return new Dictionary<string, int>(TreeOf(allianceId).Levels, StringComparer.Ordinal);
        }
    }
}
