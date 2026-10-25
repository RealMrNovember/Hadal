namespace HADAL.Shared.Protocol
{
    /// <summary>
    /// Entity id prefixes used in <c>EntityChangeDto.EntityId</c>. Wire contract between server
    /// and client: the prefix decides how a change is applied.
    /// </summary>
    public static class EntityIds
    {
        public const string Building = "building:";
        public const string Resource = "resource:";
        public const string Production = "production:";
        public const string Hero = "hero:";
        public const string Command = "command:";
        public const string Base = "base:";

        /// <summary>The wallet is a singleton entity; every frame carries the full wallet.</summary>
        public const string Wallet = Resource + "wallet";

        public const string ProductionSummary = Production + "summary";

        /// <summary>Open rings of the base (<c>BaseLayoutDto</c>); a singleton like the wallet.</summary>
        public const string BaseLayout = Base + "layout";
    }
}
