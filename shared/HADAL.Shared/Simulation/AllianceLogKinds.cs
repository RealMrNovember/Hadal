namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// The alliance's two logs (docs/31 §2), the newest <see cref="Keep"/> lines each, stored on the server.
    /// The battle log's types are worded by the client as "alliance.battle.&lt;type&gt;" with the arguments
    /// (attacker name, target name, x, y).
    /// </summary>
    public static class AllianceLogKinds
    {
        public const string Log = "log";
        public const string Battle = "battle";
        public const int Keep = 200;

        public const string AttackWon = "attack_won";
        public const string AttackLost = "attack_lost";
        public const string DefenceWon = "defence_won";
        public const string DefenceLost = "defence_lost";
        public const string Scout = "scout";
        public const string RallyWon = "rally_won";
        public const string RallyLost = "rally_lost";

        public static bool IsKind(string? kind) => kind == Log || kind == Battle;
    }
}
