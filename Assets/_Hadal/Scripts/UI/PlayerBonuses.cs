using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.StateSync;

namespace Hadal.UI
{
    /// <summary>
    /// The player's modifiers as the server's RefreshModifiers sums them: research, the command crew, VIP and the
    /// alliance's research. Screens use it to preview times (march setup, research, profile) with the same numbers.
    /// </summary>
    public static class PlayerBonuses
    {
        public static PlayerModifiers Of(ClientStateView view, GameCatalog catalog)
        {
            var modifiers = ResearchRules.Modifiers(catalog, view.ResearchLevels);
            HeroRules.AddCrew(catalog, modifiers, view.Heroes.Select(h => new OwnedHero(h.HeroId, h.Level, h.Stars, h.Shards, h.Slot)));
            VipRules.Apply(modifiers, view.Vip?.Level ?? 0);
            if (view.MyAlliance != null)
                AllianceResearchRules.Apply(modifiers, AllianceLevels(view.AllianceResearch));
            return modifiers;
        }

        private static Dictionary<string, int> AllianceLevels(AllianceResearchDto research) =>
            (research?.Nodes ?? Array.Empty<AllianceResearchNodeDto>())
            .Where(n => n != null && !string.IsNullOrEmpty(n.Id))
            .GroupBy(n => n.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Max(n => n.Level), StringComparer.Ordinal);
    }
}
