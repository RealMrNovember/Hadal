using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Protocol;

namespace Hadal.Game
{
    /// <summary>The player's avatar: one of the ready-made portraits (the heroes' painted portraits).</summary>
    public sealed partial class PlayerSession
    {
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
