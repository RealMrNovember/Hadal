using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>
    /// The player's phone notification choices (docs/36): kept on the device for the local timers and on the server
    /// for push (entity <c>EntityIds.NotificationPrefs</c>; also in the snapshot). Every field's zero value is the
    /// default (all categories on, no quiet hours), so an empty document means "never changed".
    /// </summary>
    [ProtoContract]
    public sealed class NotificationPrefsDto
    {
        /// <summary>The master switch is off: nothing is shown or pushed.</summary>
        [ProtoMember(1)] public bool Off { get; set; }

        /// <summary>Bit (1 &lt;&lt; NotificationCategory) set = that category is switched off.</summary>
        [ProtoMember(2)] public int DisabledMask { get; set; }

        /// <summary>Quiet hours on: nothing is shown between <see cref="QuietStartMinute"/> and <see cref="QuietEndMinute"/>.</summary>
        [ProtoMember(3)] public bool QuietHours { get; set; }

        /// <summary>Local minute of the day (0..1439) quiet hours start and end; the range may wrap past midnight.</summary>
        [ProtoMember(4)] public int QuietStartMinute { get; set; }
        [ProtoMember(5)] public int QuietEndMinute { get; set; }

        /// <summary>The phone's offset from UTC in minutes, so the server can place quiet hours in the player's day.</summary>
        [ProtoMember(6)] public int UtcOffsetMinutes { get; set; }

        /// <summary>The game language (Loc code) the server writes push texts in.</summary>
        [ProtoMember(7)] public string Language { get; set; } = string.Empty;

        /// <summary>Server to client: the player has saved choices on the server (a new device adopts them).</summary>
        [ProtoMember(8)] public bool Saved { get; set; }

        public NotificationPrefsDto Copy() => new()
        {
            Off = Off, DisabledMask = DisabledMask, QuietHours = QuietHours, QuietStartMinute = QuietStartMinute,
            QuietEndMinute = QuietEndMinute, UtcOffsetMinutes = UtcOffsetMinutes, Language = Language ?? string.Empty, Saved = Saved
        };

        /// <summary>Same choices (the <see cref="Saved"/> marker and the offset are not choices).</summary>
        public bool SameChoices(NotificationPrefsDto? other) =>
            other != null && Off == other.Off && DisabledMask == other.DisabledMask && QuietHours == other.QuietHours
            && QuietStartMinute == other.QuietStartMinute && QuietEndMinute == other.QuietEndMinute
            && (Language ?? string.Empty) == (other.Language ?? string.Empty);
    }
}
