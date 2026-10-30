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

        /// <summary>A profile card the player asked for (<c>PlayerProfileDto</c>).</summary>
        public const string Profile = "world:profile";

        /// <summary>Troops at home and the training queues (<c>ArmyDto</c>).</summary>
        public const string Army = Base + "army";

        /// <summary>The player's own profile (<c>PlayerProfileDto</c>; avatar choice).</summary>
        public const string MyProfile = Base + "profile";

        /// <summary>The player's gathering marches and queues (<c>MarchListDto</c>).</summary>
        public const string Marches = Base + "marches";

        /// <summary>Depth Map progress (<c>ExpeditionStateDto</c>).</summary>
        public const string Expedition = Base + "expedition";

        /// <summary>Prefix of one dive result (<c>DiveReportDto</c>), followed by the command id.</summary>
        public const string DiveReport = Base + "dive:";

        /// <summary>Prefix of one battle report (<c>BattleReportDto</c>), followed by the march id.</summary>
        public const string BattleReport = Base + "battle:";

        public const string Alliance = "alliance:";

        /// <summary>The player's alliance (<c>AllianceDto</c>); Delete when the player has none.</summary>
        public const string MyAlliance = Alliance + "mine";

        /// <summary>Browse results (<c>AllianceListDto</c>).</summary>
        public const string AllianceList = Alliance + "list";

        /// <summary>Open help requests of alliance mates (<c>HelpListDto</c>).</summary>
        public const string AllianceHelp = Alliance + "help";

        /// <summary>Prefix of a one-off "a mate helped you" note (<c>HelpReceivedDto</c>), followed by an id.</summary>
        public const string HelpReceived = Alliance + "helped:";

        /// <summary>Invitations and applications of a player without an alliance (<c>AllianceInviteListDto</c>).</summary>
        public const string AllianceInvites = Alliance + "invites";

        /// <summary>Prefix of an alliance log (<c>AllianceLogDto</c>), followed by its kind ("log" or "battle").</summary>
        public const string AllianceLog = Alliance + "log:";

        /// <summary>The player's alliance coins and this week's alliance shop (<c>AllianceCoinsDto</c>).</summary>
        public const string AllianceCoins = Alliance + "coins";

        /// <summary>The alliance's gift level and the member's gifts (<c>AllianceGiftsDto</c>).</summary>
        public const string AllianceGifts = Alliance + "gifts";

        /// <summary>The alliance research tree and the member's donations today (<c>AllianceResearchDto</c>).</summary>
        public const string AllianceResearch = Alliance + "research";

        /// <summary>Prefix of an alliance ranking (<c>AllianceRankingDto</c>), followed by its kind.</summary>
        public const string AllianceRanking = Alliance + "ranking:";

        /// <summary>VIP points, level and today's claims (<c>VipStateDto</c>).</summary>
        public const string Vip = Base + "vip";

        /// <summary>Phone notification choices (<c>NotificationPrefsDto</c>, docs/36).</summary>
        public const string NotificationPrefs = Base + "notify";

        /// <summary>The player's mailbox, newest first, with the unread count (<c>MailListDto</c>).</summary>
        public const string Mail = Base + "mail";

        /// <summary>What the player may still mail today: mails and resources (<c>MailQuotaDto</c>).</summary>
        public const string MailQuota = Base + "mailquota";

        /// <summary>Shield, beginner protection and hostile marches on the way to this player (docs/30 §5.1).</summary>
        public const string Pvp = Base + "pvp";

        /// <summary>The last PvP battles and scouts of this player (docs/30 §4.2).</summary>
        public const string WarLog = Base + "warlog";

        /// <summary>The alliance's attacks, defences and open rallies (docs/30 §5.4).</summary>
        public const string AllianceWar = Alliance + "war";

        /// <summary>Strongholds, owners and every base as a dot, for the strategic map (docs/30 §6.4).</summary>
        public const string Territory = "world:territory";

        /// <summary>The player's own kingdom (<c>KingdomDto</c>, docs/39), pushed on login.</summary>
        public const string MyKingdom = Base + "kingdom";

        /// <summary>Every kingdom (<c>KingdomListDto</c>).</summary>
        public const string Kingdoms = "world:kingdoms";

        public const string Event = "event:";

        /// <summary>Every event the player can see with their progress (<c>EventListDto</c>).</summary>
        public const string Events = Event + "list";

        /// <summary>Prefix of an event ranking (<c>EventRankingDto</c>), followed by the event id.</summary>
        public const string EventRanking = Event + "ranking:";

        /// <summary>The coming week's events, recurring ones included (<c>EventCalendarDto</c>), on request.</summary>
        public const string EventCalendar = Event + "calendar";
    }
}
