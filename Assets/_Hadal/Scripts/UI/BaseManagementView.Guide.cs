using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>The base screen's part of the first-session guide (docs/34): what the guide's "Go" opens.</summary>
    public sealed partial class BaseManagementView
    {
        private void CreateGuide()
        {
            var guide = new GameObject("OnboardingGuide");
            guide.transform.SetParent(transform, false);
            guide.AddComponent<OnboardingGuide>().Initialize(_stateSync, new OnboardingGuide.Actions
            {
                SheetOpen = () => IsOpen || _social.IsOpen || ModalOpen,
                InWorld = () => _map.IsOpen,
                OpenBuilding = id => { _social.Close(); _map.Close(); OpenHighest(id); },
                BuildNew = id => { _social.Close(); _map.Close(); OpenBuildFor(id); },
                OpenConstruction = () => { _social.Close(); _map.Close(); OpenConstructionForHelp(); },
                Heroes = () => { Close(); _social.OpenHeroes(); },
                Expand = () => { _social.Close(); _map.Close(); OpenExpand(); },
                Bag = () => { Close(); _social.OpenBag(); },
                Alliance = () => { Close(); _social.OpenAlliance(); },
                AllianceHelp = () => { Close(); _social.OpenAlliance(help: true); },
                OpenMap = () => { Close(); _social.Close(); _map.Open(); },
                Claim = id => _social.ClaimQuest(id),
                MapTarget = kind => _map.GuideTarget = kind
            });
        }

        /// <summary>The highest building of a kind; without one, the build list for it.</summary>
        private void OpenHighest(string definitionId)
        {
            var building = Buildings().Where(b => b.definitionId == definitionId).OrderByDescending(b => b.level).FirstOrDefault();
            if (string.IsNullOrEmpty(building.instanceKey))
                OpenBuildFor(definitionId);
            else
                OpenBuilding(building.instanceKey);
        }

        /// <summary>The build list on the first free slot that takes <paramref name="definitionId"/>; the expand page if none is open.</summary>
        public void OpenBuildFor(string definitionId)
        {
            var occupied = new HashSet<(int ring, int sector)>(Buildings().Select(b => (b.cellRing, b.cellSector)));
            var wallet = Wallet();
            var owned = OwnedCounts();
            var reactor = ReactorLevel();
            for (var ring = 1; ring <= _grid.UnlockedRingCount; ring++)
            {
                for (var sector = 0; sector < _catalog.GetSectorCount(ring); sector++)
                {
                    var verdict = PlacementRules.Validate(_catalog, definitionId, ring, sector, _grid.UnlockedRingCount, occupied, wallet, owned, reactor);
                    // A slot that can take it (maybe not yet affordable: the list says why).
                    if (verdict is PlacementRejectReason.RingNotAllowed or PlacementRejectReason.SlotTypeMismatch or PlacementRejectReason.SectorRestricted
                        or PlacementRejectReason.SlotDoesNotExist or PlacementRejectReason.SlotOccupied or PlacementRejectReason.UnknownBuilding
                        or PlacementRejectReason.RingLocked or PlacementRejectReason.SectorLocked)
                        continue;

                    _ring = ring;
                    _sector = sector;
                    _buildingKey = string.Empty;
                    _expandOpen = false;
                    _researchOpen = false;
                    _confirmRemove = string.Empty;
                    _sheet.gameObject.SetActive(true);
                    Render();
                    return;
                }
            }

            OpenExpand();
        }

        /// <summary>
        /// Somewhere to ask for help: a construction past the free-finish window (its panel has the help button), else
        /// the Research Center (a study runs long enough), else the build list for one.
        /// </summary>
        private void OpenConstructionForHelp()
        {
            var now = Time.realtimeSinceStartup;
            var running = Buildings().FirstOrDefault(b => b.IsUnderConstruction && b.constructionEndsAt - now > _catalog.Progression.freeFinishSeconds);
            if (!string.IsNullOrEmpty(running.instanceKey))
            {
                OpenBuilding(running.instanceKey);
                return;
            }

            if (Buildings().Any(b => b.definitionId == ResearchRules.CenterId && b.level > 0))
                OpenResearchFromHud();
            else
                OpenBuildFor(ResearchRules.CenterId);
        }
    }
}
