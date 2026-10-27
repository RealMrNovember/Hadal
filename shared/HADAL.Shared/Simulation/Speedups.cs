using System;

namespace HADAL.Shared.Simulation
{
    /// <summary>What a speed-up works on.</summary>
    public enum SpeedupKind
    {
        Unknown = 0,

        /// <summary>Any timer: construction, training, healing, research.</summary>
        Any = 1,
        Build = 2,
        Train = 3,
        Heal = 4,
        Research = 5,

        /// <summary>A march on the map: takes a share of the time left (Last Asylum's 25% / 50%).</summary>
        March = 6
    }

    /// <summary>Why a speed-up was refused. Sent in <c>CommandAckDto.Reason</c>.</summary>
    public enum SpeedupRejectReason
    {
        None = 0,
        UnknownItem = 1,
        NotEnoughItems = 2,
        WrongTarget = 3,
        NothingToSpeedUp = 4
    }

    /// <summary>
    /// Speed-up items in the bag, read from their id like resource capsules: "speed_any_60" takes 60 minutes off any
    /// timer, "speed_train_15" off training only, "speed_march_p50" takes half the time left off a march. No catalog
    /// entry is needed, so chests, quests, coupons and the Overseer can hand out any size.
    /// </summary>
    public static class Speedups
    {
        public const string Prefix = "speed_";

        public static string Id(SpeedupKind kind, int value, bool percent = false) =>
            $"{Prefix}{kind.ToString().ToLowerInvariant()}_{(percent ? "p" : string.Empty)}{value}";

        /// <param name="value">Minutes, or a percent of the time left when <paramref name="percent"/>.</param>
        public static bool TryParse(string? id, out SpeedupKind kind, out int value, out bool percent)
        {
            kind = SpeedupKind.Unknown;
            value = 0;
            percent = false;
            if (string.IsNullOrEmpty(id) || !id!.StartsWith(Prefix, StringComparison.Ordinal))
                return false;
            var parts = id.Substring(Prefix.Length).Split('_');
            if (parts.Length != 2 || !Enum.TryParse(parts[0], true, out kind) || kind == SpeedupKind.Unknown)
                return false;
            percent = parts[1].StartsWith("p", StringComparison.Ordinal);
            if (!int.TryParse(percent ? parts[1].Substring(1) : parts[1], out value) || value <= 0)
                return false;
            // Marches take shares of the time left; everything else takes minutes.
            return percent ? kind == SpeedupKind.March && value < 100 : kind != SpeedupKind.March;
        }

        /// <summary>Does a speed-up of <paramref name="kind"/> work on a <paramref name="target"/> timer?</summary>
        public static bool Fits(SpeedupKind kind, SpeedupKind target) =>
            kind == target || (kind == SpeedupKind.Any && target is SpeedupKind.Build or SpeedupKind.Train or SpeedupKind.Heal or SpeedupKind.Research);

        /// <summary>Seconds <paramref name="count"/> items take off a timer with <paramref name="left"/> seconds left.</summary>
        public static double Cut(int value, bool percent, int count, double left)
        {
            if (left <= 0)
                return 0;
            if (!percent)
                return Math.Min(left, value * 60.0 * count);
            var remaining = left;
            for (var i = 0; i < count; i++)
                remaining *= 1 - value / 100.0;
            return left - remaining;
        }
    }
}
