using System;
using System.Text;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// The mailbox (docs/29 §2.1). Mails live 30 days; unclaimed attachments go with them.
    /// <para>
    /// Players mail one player at a time (talking to everyone is the world chat's job; only the Overseer mails all).
    /// Resources may go along only to a mate in the same alliance (the genre's "resource help"), each resource with its
    /// own daily allowance per sender, counted per UTC day. Hadalite (bought) and Superalloy (scarce) never travel.
    /// The allowance grows with the sender's Core Reactor so fresh accounts cannot feed a main:
    /// </para>
    /// <code>
    ///   resource            full allowance / day   reactor 5 or lower   reactor 15   reactor 25+
    ///   Titanium, Biomass,  500,000                100,000 (1/5)        300,000      500,000
    ///   Food
    ///   Oxygen, Energy      200,000                 40,000              120,000      200,000
    ///   Hadalite, Alloy     never
    ///   mails sent          30 a day (any kind)
    /// </code>
    /// Allowance = full × clamp(reactor, 5, 25) / 25.
    /// </summary>
    public static class MailRules
    {
        public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

        public const int MaxTitle = 80;

        /// <summary>Overseer and system mails.</summary>
        public const int MaxBody = 2000;

        /// <summary>Mails written by players.</summary>
        public const int MaxPlayerBody = 1000;

        public const int MaxSender = 40;

        /// <summary>The newest this many mails are shown; older ones stay stored until they expire.</summary>
        public const int MaxShown = 200;

        /// <summary>Mails one player may send per UTC day (spam cap), with or without resources.</summary>
        public const int MailsPerDay = 30;

        /// <summary>The Core Reactor level from which the full allowance applies; below it the allowance shrinks.</summary>
        public const int FullAllowanceLevel = 25;

        /// <summary>The allowance never drops below this level's share (1/5).</summary>
        public const int MinAllowanceLevel = 5;

        /// <summary>Resources players may send, in display order.</summary>
        public static readonly ResourceType[] Sendable =
        {
            ResourceType.Titanium, ResourceType.Biomass, ResourceType.Food, ResourceType.Oxygen, ResourceType.Energy
        };

        public static bool CanSend(ResourceType resource) => Array.IndexOf(Sendable, resource) >= 0;

        /// <summary>The full daily allowance of a resource (0 = never sendable).</summary>
        public static long FullAllowance(ResourceType resource) => resource switch
        {
            ResourceType.Titanium or ResourceType.Biomass or ResourceType.Food => 500_000,
            ResourceType.Oxygen or ResourceType.Energy => 200_000,
            _ => 0
        };

        /// <summary>What a sender with this Core Reactor level may send of a resource per day.</summary>
        public static long DailyAllowance(ResourceType resource, int reactorLevel)
        {
            var level = Math.Max(MinAllowanceLevel, Math.Min(FullAllowanceLevel, reactorLevel));
            return FullAllowance(resource) * level / FullAllowanceLevel;
        }

        // --- Tabs (docs/37) ---------------------------------------------------------------------

        public const string TabBattle = "battle";
        public const string TabAlliance = "alliance";
        public const string TabEvent = "event";
        public const string TabSystem = "system";
        public const string TabPersonal = "personal";

        /// <summary>The mailbox tabs in display order, as in Last Asylum: battle and scout reports first.</summary>
        public static readonly string[] Tabs = { TabBattle, TabAlliance, TabEvent, TabSystem, TabPersonal };

        /// <summary>
        /// The tab a mail of this kind sits in: battle reports (scouts and shield-blocked attacks too), alliance, events
        /// and boss prizes, player mail; the game's and the Overseer's notices (and any kind not known yet) under system.
        /// </summary>
        public static string TabOf(string? kind) => kind switch
        {
            "battle" => TabBattle,
            "alliance" => TabAlliance,
            "event" => TabEvent,
            "player" => TabPersonal,
            _ => TabSystem
        };

        /// <summary>Whether a mail of this kind belongs to the tab; an empty tab means the whole box.</summary>
        public static bool InTab(string? kind, string? tab) => string.IsNullOrEmpty(tab) || TabOf(kind) == tab;

        /// <summary>The UTC day number allowances are counted in.</summary>
        public static long Day(long unixMs) => unixMs / 86_400_000L;

        public static long Day(DateTimeOffset at) => Day(at.ToUnixTimeMilliseconds());

        /// <summary>Trims and drops control characters; the body keeps its line breaks. Cut to <paramref name="max"/>.</summary>
        public static string Clean(string? text, int max, bool multiline)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            var builder = new StringBuilder(text!.Length);
            foreach (var c in text)
            {
                if (c == '\n' && multiline)
                    builder.Append(c);
                else if (char.IsControl(c))
                    builder.Append(multiline && c == '\r' ? '\0' : ' ');
                else
                    builder.Append(c);
            }

            var cleaned = builder.Replace("\0", string.Empty).ToString().Trim();
            return cleaned.Length <= max ? cleaned : cleaned.Substring(0, max);
        }
    }
}
