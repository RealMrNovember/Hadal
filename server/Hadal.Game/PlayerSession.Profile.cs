using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>The player's avatar (one of the heroes' painted portraits) and name changes (RenameRules).</summary>
    public sealed partial class PlayerSession
    {
        /// <summary>Pays for and applies a new name on the live session; null when done, else the reason.</summary>
        internal string? Rename(string name, DateTimeOffset now)
        {
            lock (_gate)
            {
                var hadCard = _state.Items.TryGetValue(RenameRules.Item, out var cards) && cards > 0;
                var hadalite = _state.Wallet.TryGetValue(ResourceType.Hadalite, out var h) ? h : 0;
                var reason = PayRename(_state, now);
                if (reason != null)
                    return reason;
                _displayName = name;
                var changes = new List<EntityChangeDto>
                {
                    new()
                    {
                        EntityId = EntityIds.MyProfile,
                        Kind = EntityChangeKind.Update,
                        Payload = _serializer.Serialize(new PlayerProfileDto { PlayerId = PublicId, Name = name, AvatarId = _state.AvatarId })
                    }
                };
                if (hadCard && (_state.Items.TryGetValue(RenameRules.Item, out var left) ? left : 0) < cards)
                    changes.Add(InventoryChange());
                if ((_state.Wallet.TryGetValue(ResourceType.Hadalite, out var after) ? after : 0) != hadalite)
                {
                    Score(EventActions.HadaliteSpent, hadalite - after);
                    changes.Add(WalletChange());
                }

                SendDelta(now, changes);
                return null;
            }
        }

        /// <summary>
        /// The rules of a name change on a player's state: a day between changes, the first free, then a Rename Card
        /// or its Hadalite price. Null when paid (the state is marked dirty), else the reason.
        /// </summary>
        internal static string? PayRename(PlayerState state, DateTimeOffset now)
        {
            // The free changes come without a wait; the day between changes applies to paid ones.
            if (state.RenameCount >= RenameRules.FreeRenames)
            {
                if (state.RenamedAt is { } last && now - last < RenameRules.Cooldown)
                    return "RenameCooldown";
                if (state.Items.TryGetValue(RenameRules.Item, out var cards) && cards > 0)
                {
                    if (cards == 1)
                        state.Items.Remove(RenameRules.Item);
                    else
                        state.Items[RenameRules.Item] = cards - 1;
                }
                else if ((state.Wallet.TryGetValue(ResourceType.Hadalite, out var hadalite) ? hadalite : 0) >= RenameRules.HadalitePrice)
                {
                    state.Wallet[ResourceType.Hadalite] = hadalite - RenameRules.HadalitePrice;
                }
                else
                {
                    return "NeedRenameCard";
                }
            }

            state.RenameCount++;
            state.RenamedAt = now;
            state.IsDirty = true;
            return null;
        }

        private void SetProfile(SetProfileCommand command, DateTimeOffset now)
        {
            var avatar = command.AvatarId ?? string.Empty;
            if (avatar.Length > 0 && _catalog.Heroes.All(h => h.Portrait != avatar))
            {
                Reject(command.CommandId, command.ClientSequence, "UnknownAvatar", now);
                return;
            }

            _state.AvatarId = avatar;
            _state.IsDirty = true;
            SendDelta(now, new List<EntityChangeDto>
            {
                new()
                {
                    EntityId = EntityIds.MyProfile,
                    Kind = EntityChangeKind.Update,
                    Payload = _serializer.Serialize(new PlayerProfileDto { PlayerId = PublicId, Name = _displayName, AvatarId = avatar })
                },
                Accepted(command.CommandId, command.ClientSequence)
            });
        }
    }
}
