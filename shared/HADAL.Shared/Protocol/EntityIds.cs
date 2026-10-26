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

        /// <summary>Research levels and running researches (<c>ResearchStateDto</c>).</summary>
        public const string Research = Base + "research";

        /// <summary>Prefix of one-off salvage results (<c>SalvageReportDto</c>), followed by the building id.</summary>
        public const string SalvageReport = Base + "salvage:";

        /// <summary>Owned heroes, crew and Cryo-Pod state (<c>HeroRosterDto</c>).</summary>
        public const string Heroes = Base + "heroes";

        /// <summary>Prefix of one-off Cryo-Pod results (<c>GachaResultDto</c>), followed by the command id.</summary>
        public const string GachaResult = Base + "gacha:";

        /// <summary>Chapter task, daily tasks and activity chests (<c>QuestStateDto</c>).</summary>
        public const string Quests = Base + "quests";

        /// <summary>Bag items and hero cards (<c>InventoryDto</c>).</summary>
        public const string Inventory = Base + "inventory";

        /// <summary>Prefix of one-off reward lists (<c>RewardReportDto</c>), followed by the command id.</summary>
        public const string RewardReport = Base + "reward:";

        public const string Chat = "chat:";

        /// <summary>Recent messages of every channel the player can read, sent on login (<c>ChatHistoryDto</c>).</summary>
        public const string ChatHistory = Chat + "history";

        /// <summary>Prefix of one new chat line (<c>ChatMessageDto</c>), followed by its id.</summary>
        public const string ChatMessage = Chat + "msg:";

        /// <summary>Bases around the tile the player looks at on the world map (<c>WorldViewDto</c>).</summary>
        public const string WorldView = "world:view";

        public const string Alliance = "alliance:";

        /// <summary>The player's alliance (<c>AllianceDto</c>); Delete when the player has none.</summary>
        public const string MyAlliance = Alliance + "mine";

        /// <summary>Browse results (<c>AllianceListDto</c>).</summary>
        public const string AllianceList = Alliance + "list";

        /// <summary>Open help requests of alliance mates (<c>HelpListDto</c>).</summary>
        public const string AllianceHelp = Alliance + "help";
    }
}
