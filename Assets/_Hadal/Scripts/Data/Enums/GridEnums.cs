namespace Hadal.Data.Enums
{
    // GridSlotType lives in HADAL.Shared.Enums — the server validates placement with it.

    public enum GridHighlightState
    {
        None,
        Selected,
        Valid,
        Invalid,
        Occupied
    }

    public enum BuildModeState
    {
        Idle,
        Placing,
        Destroying
    }
}
