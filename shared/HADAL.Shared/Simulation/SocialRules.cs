using System;
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
    /// Alliance ranks and what each may do (docs/rehber 10 §3): R5 Leader, R4 Officer, R3 Veteran, R2 Diver,
    /// R1 Recruit. A captain only changes or removes ranks below their own.
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

        /// <summary>Optional duty tags an officer carries; they show who to ask, they grant nothing.</summary>
        public static readonly string[] Roles = { "", "war", "logistics", "diplomacy", "scout" };

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

        /// <summary>The hub's announcement: officers (R4) and the leader.</summary>
        public static bool CanEditAnnouncement(int actor) => actor >= Officer;
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
        /// <summary>Trims, drops control characters and collapses whitespace runs; cut to the catalog length.</summary>
        public static string CleanMessage(string? text, ChatCatalogData rules)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var builder = new StringBuilder(text!.Length);
            var lastSpace = false;
            foreach (var c in text)
            {
                if (char.IsControl(c) || char.IsWhiteSpace(c))
                {
                    if (!lastSpace && builder.Length > 0)
                        builder.Append(' ');
                    lastSpace = true;
                    continue;
                }

                builder.Append(c);
                lastSpace = false;
            }

            var cleaned = builder.ToString().Trim();
            var max = Math.Max(1, rules.maxLength);
            return cleaned.Length <= max ? cleaned : cleaned.Substring(0, max);
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
            return joined.Length <= MaxAnnouncement ? joined : joined.Substring(0, MaxAnnouncement);
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
