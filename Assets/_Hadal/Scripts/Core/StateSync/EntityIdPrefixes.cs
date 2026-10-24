namespace Hadal.Core.StateSync
{
    public static class EntityIdPrefixes
    {
        public const string Building = "building:";
        public const string Resource = "resource:";
        public const string Production = "production:";
        public const string Hero = "hero:";
        public const string Command = "command:";

        /// <summary>
        /// The player's wallet and production summary are singletons, so they replicate
        /// under fixed entity ids rather than per-instance ones.
        /// </summary>
        public const string WalletEntityId = Resource + "wallet";

        public const string ProductionEntityId = Production + "summary";
    }
}
