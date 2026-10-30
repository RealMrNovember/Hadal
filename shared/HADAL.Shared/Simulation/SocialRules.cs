using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HADAL.Shared.Catalog;

namespace HADAL.Shared.Simulation
{
    /// <summary>Why a chat or alliance command was refused. Sent in <c>CommandAckDto.Reason</c>.</summary>
    public enum SocialRejectReason
    {
        None = 0,
        EmptyMessage = 1,
        TooFast = 2,
        NotInAlliance = 3,
        AlreadyInAlliance = 4,
        InvalidName = 5,
        InvalidTag = 6,
        NameTaken = 7,
        TagTaken = 8,
        InsufficientResources = 9,
        UnknownAlliance = 10,
        AllianceFull = 11,
        NotLeader = 12,
        UnknownMember = 13,
        NothingToHelp = 14,
        AlreadyRequested = 15,
        Muted = 16,
        NotAllowed = 17,
        InviteOnly = 18,
        AlreadyInvited = 19,
        NoInvite = 20,
        RankFull = 21,
        UnknownPlayer = 22,
        AlreadyApplied = 23,
        NoApplication = 24,
        SoldOut = 25,
        NotEnoughCoins = 26,
        NothingToCollect = 27,
        DonationLimit = 28,

        /// <summary>A poll or time-slot vote that breaks AlliancePollRules (question, options, hours, slots).</summary>
        InvalidPoll = 29,
        UnknownPoll = 30,
        PollClosed = 31,

        /// <summary>The alliance already has AlliancePollRules.MaxOpen open votes.</summary>
        TooManyPolls = 32,

        /// <summary>A ballot with no, unknown or repeated options, or several on a single-choice poll.</summary>
        InvalidVote = 33,
    }

    /// <summary>Chat channels. Values are a wire contract.</summary>
    public enum ChatChannel
    {
        World = 0,
        Alliance = 1,

        /// <summary>One-to-one messages; the line names the other player.</summary>
        Private = 2,
    }

    /// <summary>Who may join an alliance (docs/rehber 10 §2). Values are a wire contract.</summary>
    public enum AllianceJoinMode
    {
        Open = 0,
        Approval = 1,
        Closed = 2,
    }

    /// <summary>
    /// Alliance ranks and what each may do (docs/rehber 10 §3, docs/29 §2.3): R5 Leader, R4 Officer, R3 Veteran, R2 Diver,
    /// R1 Recruit. A captain only changes or removes ranks below their own. The leader may do everything; an officer
    /// carries one duty and gets that duty's rights: war runs alliance events (time-slot votes now; the Lure Trap and its
    /// difficulty later), logistics places alliance buildings, diplomacy runs polls and writes the announcement, scout
    /// will mark targets once map marks exist. An officer without a duty keeps the plain officer rights only.
    /// </summary>
    public static class AlliancePermissions
    {
        public const int Leader = 5;
        public const int Officer = 4;
        public const int Veteran = 3;
        public const int Diver = 2;
        public const int Recruit = 1;

        /// <summary>Officer seats (the guide grows them with the alliance level; level 1 has four).</summary>
        public const int MaxOfficers = 4;

        /// <summary>A recruit becomes a diver by itself after this long.</summary>
        public const int RecruitHours = 48;

        public const string War = "war";
        public const string Logistics = "logistics";
        public const string Diplomacy = "diplomacy";
        public const string Scout = "scout";

        /// <summary>The duty an officer carries ("" none). Several officers may share one.</summary>
        public static readonly string[] Roles = { "", War, Logistics, Diplomacy, Scout };

        /// <summary>A leader offline this long hands over by itself (docs/29 §5).</summary>
        public const int InactiveLeaderDays = 14;

        /// <summary>An heir must have been online within this many days.</summary>
        public const int ActiveHeirDays = 3;

        /// <summary>Whether <paramref name="rank"/> with duty <paramref name="role"/> holds <paramref name="duty"/>'s rights.</summary>
        public static bool HasDuty(int rank, string? role, string duty) => rank == Leader || (rank == Officer && role == duty);

        /// <summary>War: alliance events — time-slot votes now; scheduling the Lure Trap and its difficulty later.</summary>
        public static bool CanRunEvents(int rank, string? role) => HasDuty(rank, role, War);

        /// <summary>Logistics: plant the hub, place and remove beacons and alliance buildings.</summary>
        public static bool CanBuild(int rank, string? role) => HasDuty(rank, role, Logistics);

        /// <summary>Diplomacy: polls (and the announcement, <see cref="CanEditAnnouncement"/>).</summary>
        public static bool CanRunPolls(int rank, string? role) => HasDuty(rank, role, Diplomacy);

        /// <summary>Scout: target marks on the map. No marks exist yet; reserved for them.</summary>
        public static bool CanMarkTargets(int rank, string? role) => HasDuty(rank, role, Scout);

        /// <summary>
        /// Who takes over from a leader gone <see cref="InactiveLeaderDays"/> (docs/29 §5): the most powerful officer seen
        /// within <see cref="ActiveHeirDays"/>; without one the most powerful active member of the next rank down. Null
        /// when nobody is active. Equal power goes to the member listed first (the longest-standing).
        /// </summary>
        public static string? InactiveLeaderHeir(IEnumerable<(string Id, int Rank, long Power, bool Active)> members, string leaderId) =>
            members.Select((m, i) => (m, i))
                .Where(x => x.m.Id != leaderId && x.m.Active && x.m.Rank < Leader)
                .OrderByDescending(x => x.m.Rank).ThenByDescending(x => x.m.Power).ThenBy(x => x.i)
                .Select(x => x.m.Id).FirstOrDefault();

        public static bool CanInvite(int rank) => rank >= Veteran;

        public static bool CanApprove(int rank) => rank >= Veteran;

        public static bool CanKick(int actor, int target) =>
            actor == Leader ? target < Leader : actor == Officer && target <= Veteran;

        /// <summary>The leader moves anyone below to R1–R4; an officer moves R1–R3 within R1–R3.</summary>
        public static bool CanSetRank(int actor, int target, int rank) =>
            rank >= Recruit && (actor == Leader ? target < Leader && rank <= Officer : actor == Officer && target <= Veteran && rank <= Veteran);

        public static bool CanSetRole(int actor) => actor == Leader;

        public static bool CanEditSettings(int actor) => actor == Leader;

        public static bool CanTransfer(int actor) => actor == Leader;

        /// <summary>The hub's announcement: the leader and diplomacy officers.</summary>
        public static bool CanEditAnnouncement(int actor, string? role) => HasDuty(actor, role, Diplomacy);

        /// <summary>The alliance research focus: the leader and every officer.</summary>
        public static bool CanSetResearchFocus(int actor) => actor >= Officer;
    }

    /// <summary>Chat and alliance rules shared by server and client (docs/25_Heroes_Alliance_Chat.md).</summary>
    /// <summary>
    /// What alliance mates can help (docs/25 §4): a construction, the Med Bay's healing, a research, an emergency repair. Each help cuts the
    /// same share of the job's total time (SocialRules.HelpSeconds), at most the catalog's maxHelps times.
    /// </summary>
    public static class HelpKinds
    {
        public const string Build = "build";
        public const string Heal = "heal";
        public const string Research = "research";

        /// <summary>The repair of an emergency (docs/35); target = the emergency id.</summary>
        public const string Repair = "repair";

        /// <summary>HelpAll filter for construction and research together (the clasped-hands bubble).</summary>
        public const string General = "general";

        /// <summary>Empty (older clients) means build.</summary>
        public static string Normalize(string? kind) => string.IsNullOrEmpty(kind) ? Build : kind!;

        public static bool IsValid(string kind) => kind == Build || kind == Heal || kind == Research || kind == Repair;

        /// <summary>Whether a request of <paramref name="kind"/> is in the HelpAll group <paramref name="filter"/>.</summary>
        public static bool Matches(string? filter, string kind) =>
            string.IsNullOrEmpty(filter) || (filter == General ? Normalize(kind) != Heal : Normalize(kind) == filter);
    }

    public static class SocialRules
    {
        /// <summary>
        /// UTF-16 units a message may spend per allowed character: emoji take 2 (one surrogate pair) up to about 11
        /// (a ZWJ family); the length limit counts characters as the player sees them, this bounds the storage.
        /// </summary>
        public const int MaxUnitsPerElement = 4;

        /// <summary>
        /// Trims, drops control characters and unpaired surrogates, collapses whitespace runs; cut to the catalog
        /// length in text elements (an emoji counts once) and to <see cref="MaxUnitsPerElement"/> UTF-16 units each.
        /// Emoji and every other printable character stay.
        /// </summary>
        public static string CleanMessage(string? text, ChatCatalogData rules)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var builder = new StringBuilder(text!.Length);
            var lastSpace = false;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (char.IsControl(c) || char.IsWhiteSpace(c))
                {
                    if (!lastSpace && builder.Length > 0)
                        builder.Append(' ');
                    lastSpace = true;
                    continue;
                }

                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    builder.Append(c).Append(text[++i]);
                    lastSpace = false;
                    continue;
                }

                if (char.IsSurrogate(c))
                    continue; // half an emoji: UTF-8 (the database, the wire) cannot carry it

                builder.Append(c);
                lastSpace = false;
            }

            return CutToTextElements(builder.ToString().Trim(), Math.Max(1, rules.maxLength));
        }

        /// <summary>Characters as the player sees them: an emoji (even a ZWJ sequence or a flag) is one.</summary>
        public static int TextLength(string? text) => string.IsNullOrEmpty(text) ? 0 : new StringInfo(text).LengthInTextElements;

        /// <summary>
        /// The first <paramref name="max"/> text elements, never splitting an emoji, and at most
        /// <paramref name="max"/> x <see cref="MaxUnitsPerElement"/> UTF-16 units.
        /// </summary>
        public static string CutToTextElements(string? text, int max)
        {
            if (string.IsNullOrEmpty(text) || max <= 0)
                return string.Empty;
            if (text!.Length <= max)
                return text;

            var maxUnits = max * MaxUnitsPerElement;
            var elements = StringInfo.GetTextElementEnumerator(text);
            var count = 0;
            var end = 0;
            while (elements.MoveNext())
            {
                var next = elements.ElementIndex + elements.GetTextElement().Length;
                if (count == max || next > maxUnits)
                    break;
                count++;
                end = next;
            }

            return end >= text.Length ? text : text.Substring(0, end);
        }

        public static string CleanName(string? name) => CleanMessage(name, new ChatCatalogData { maxLength = 64 });

        public static bool IsValidName(string? name, AllianceCatalogData rules)
        {
            var cleaned = CleanName(name);
            if (cleaned.Length < rules.nameMin || cleaned.Length > rules.nameMax || cleaned != (name ?? string.Empty).Trim())
                return false;
            foreach (var c in cleaned)
            {
                if (!(char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_' || c == '.'))
                    return false;
            }

            return true;
        }

        /// <summary>Tags are upper-case letters and digits (any script letters allowed, upper-cased).</summary>
        public static string NormalizeTag(string? tag) => (tag ?? string.Empty).Trim().ToUpperInvariant();

        public static bool IsValidTag(string? tag, AllianceCatalogData rules)
        {
            var normalized = NormalizeTag(tag);
            if (normalized.Length < rules.tagMin || normalized.Length > rules.tagMax)
                return false;
            foreach (var c in normalized)
            {
                if (!char.IsLetterOrDigit(c))
                    return false;
            }

            return true;
        }

        /// <summary>Alliance banner icons (HUD icon ids) and colours; a banner is "icon:colour" indices.</summary>
        public static readonly string[] EmblemIcons = { "side_alliance", "event_war", "power", "nav_world", "quest", "event_gift", "left_research", "res_hadalite" };

        public static readonly string[] EmblemColors = { "2E9BD6", "2BB673", "D64545", "E0A21B", "8E5BD8", "D6589E", "3FBFBF", "6C7A89" };

        public const string DefaultEmblem = "0:0";

        public const int MaxAnnouncement = 500;

        /// <summary>The announcement keeps its line breaks; other control characters go. Cut to MaxAnnouncement.</summary>
        public static string CleanAnnouncement(string? text)
        {
            var lines = (text ?? string.Empty).Replace("\r", string.Empty).Split('\n')
                .Select(l => CleanMessage(l, new ChatCatalogData { maxLength = MaxAnnouncement }));
            var joined = string.Join("\n", lines).Trim();
            return joined.Length <= MaxAnnouncement ? joined : CutToTextElements(joined, MaxAnnouncement);
        }

        /// <summary>A UI language code such as "tr" or "zh-Hans".</summary>
        public static bool IsLanguageCode(string? code) =>
            !string.IsNullOrEmpty(code) && code!.Length <= 8 && code.All(c => char.IsLetter(c) || c == '-');

        public static bool IsValidEmblem(string? emblem)
        {
            var parts = (emblem ?? string.Empty).Split(':');
            return parts.Length == 2 && int.TryParse(parts[0], out var i) && int.TryParse(parts[1], out var c)
                   && i >= 0 && i < EmblemIcons.Length && c >= 0 && c < EmblemColors.Length;
        }

        /// <summary>Seconds one help takes off a construction of <paramref name="totalSeconds"/>.</summary>
        public static int HelpSeconds(AllianceCatalogData rules, int totalSeconds) =>
            Math.Max(Math.Max(1, rules.helpMinSeconds), totalSeconds * Math.Max(0, rules.helpPercent) / 100);
    }
}
