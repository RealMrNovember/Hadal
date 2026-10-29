using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Game.Persistence;
using Xunit;

namespace Hadal.Game.Tests
{
    public sealed class RenameTests
    {
        private static readonly DateTimeOffset Start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

        [Fact]
        public void The_first_change_is_free_then_a_card_or_hadalite_a_day_apart()
        {
            var state = new PlayerState();
            Assert.Null(PlayerSession.PayRename(state, Start));
            Assert.Equal("RenameCooldown", PlayerSession.PayRename(state, Start.AddHours(23)));
            Assert.Equal("NeedRenameCard", PlayerSession.PayRename(state, Start.AddDays(1)));

            state.Items[RenameRules.Item] = 1;
            Assert.Null(PlayerSession.PayRename(state, Start.AddDays(1)));
            Assert.False(state.Items.ContainsKey(RenameRules.Item));

            state.Wallet[ResourceType.Hadalite] = RenameRules.HadalitePrice + 7;
            Assert.Null(PlayerSession.PayRename(state, Start.AddDays(2)));
            Assert.Equal(7, state.Wallet[ResourceType.Hadalite]);
            Assert.Equal(3, state.RenameCount);
        }

        [Fact]
        public void Name_changes_survive_a_save()
        {
            var state = new PlayerState();
            PlayerSession.PayRename(state, Start);
            var copy = new PlayerState();
            PlayerExtras.Read(PlayerExtras.Write(state), copy);
            Assert.Equal((1, (DateTimeOffset?)Start), (copy.RenameCount, copy.RenamedAt));
        }
    }
}
