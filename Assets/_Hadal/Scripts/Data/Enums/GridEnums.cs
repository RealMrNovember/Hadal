namespace Hadal.Data.Enums
{
    /// <summary>
    /// Defines what category of structure may occupy a polar grid slot.
    /// </summary>
    public enum GridSlotType
    {
        Core = 0,
        Production = 1,
        Research = 2,
        Military = 3,
        MegaDome = 4,
        Alliance = 5,
        Decoration = 6,
        Universal = 99
    }

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
