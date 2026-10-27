using System;
using System.Linq;
using HADAL.Shared.Catalog;

namespace HADAL.Shared.Simulation
{
    public enum DiveRejectReason
    {
        None = 0,
        UnknownStage = 1,
        Locked = 2,
        ReactorLevelTooLow = 3,
        NotEnoughOxygen = 4
    }

    /// <summary>
    /// The Depth Map campaign (docs/rehber 08 §5): 10 chapters of 10 stages, cleared in order. Until troops and
    /// the battle resolver exist a dive is won when the player's power reaches the stage's recommended power.
    /// </summary>
    public static class ExpeditionRules
    {
        public static ExpeditionChapterData? Chapter(ExpeditionCatalogData expedition, int chapter) =>
            expedition.chapters.FirstOrDefault(c => c.chapter == chapter);

        /// <summary>Stages cleared before this one, counting from chapter 1 stage 1.</summary>
        public static int Index(ExpeditionCatalogData expedition, int chapter, int stage) => (chapter - 1) * expedition.stages + (stage - 1);

        public static bool IsBoss(ExpeditionCatalogData expedition, int stage) => stage == expedition.stages;

        /// <summary>Recommended power: the chapter's first to last stage climb evenly; the boss stands apart.</summary>
        public static long RecommendedPower(ExpeditionCatalogData expedition, ExpeditionChapterData chapter, int stage)
        {
            if (IsBoss(expedition, stage))
                return chapter.powerBoss;
            var steps = Math.Max(1, expedition.stages - 2);
            return chapter.powerFirst + (chapter.powerLast - chapter.powerFirst) * (stage - 1) / steps;
        }

        public static DiveRejectReason Validate(ExpeditionCatalogData expedition, int chapter, int stage, int cleared, int reactorLevel, long oxygen)
        {
            var data = Chapter(expedition, chapter);
            if (data == null || stage < 1 || stage > expedition.stages)
                return DiveRejectReason.UnknownStage;
            if (reactorLevel < data.reactor)
                return DiveRejectReason.ReactorLevelTooLow;
            if (Index(expedition, chapter, stage) > cleared)
                return DiveRejectReason.Locked;
            if (oxygen < data.oxygen)
                return DiveRejectReason.NotEnoughOxygen;
            return DiveRejectReason.None;
        }

        /// <summary>Income hours one rewarded dive pays (docs/rehber 15 §3.4).</summary>
        public static double RewardIncomeHours(ExpeditionCatalogData expedition, int reactorLevel) =>
            (reactorLevel < expedition.lateFromReactor ? expedition.gsPerDayEarly : expedition.gsPerDayLate) / Math.Max(1, expedition.dailyRewarded);

        public static long Refund(ExpeditionCatalogData expedition, ExpeditionChapterData chapter) => chapter.oxygen * expedition.refundPercent / 100;
    }
}
