namespace HADAL.Shared.Serialization
{
    public static class SchemaVersion
    {
        // 2: server → client frames are wrapped in ServerMessage (v1 sent bare, ambiguous envelopes).
        public const uint Current = 2;
        public const uint MinSupported = 2;
    }
}
