using System;
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
    }

    /// <summary>Chat channels. Values are a wire contract.</summary>
    public enum ChatChannel
    {
        World = 0,
        Alliance = 1,
    }

    /// <summary>Chat and alliance rules shared by server and client (docs/25_Heroes_Alliance_Chat.md).</summary>
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

        /// <summary>Seconds one help takes off a construction of <paramref name="totalSeconds"/>.</summary>
        public static int HelpSeconds(AllianceCatalogData rules, int totalSeconds) =>
            Math.Max(Math.Max(1, rules.helpMinSeconds), totalSeconds * Math.Max(0, rules.helpPercent) / 100);
    }
}
