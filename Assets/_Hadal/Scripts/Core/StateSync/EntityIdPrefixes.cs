using HADAL.Shared.Protocol;

namespace Hadal.Core.StateSync
{
    /// <summary>Client alias of <see cref="EntityIds"/> — the values are owned by HADAL.Shared.</summary>
    public static class EntityIdPrefixes
    {
        public const string Building = EntityIds.Building;
        public const string Resource = EntityIds.Resource;
        public const string Production = EntityIds.Production;
        public const string Hero = EntityIds.Hero;
        public const string Command = EntityIds.Command;
        public const string Base = EntityIds.Base;

        /// <summary>
        /// The player's wallet and production summary are singletons, so they replicate
        /// under fixed entity ids rather than per-instance ones.
        /// </summary>
        public const string WalletEntityId = EntityIds.Wallet;

        public const string ProductionEntityId = EntityIds.ProductionSummary;
    }
}
