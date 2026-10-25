namespace HADAL.Shared.Serialization
{
    public static class SchemaVersion
    {
        // 2: server → client frames are wrapped in ServerMessage (v1 sent bare, ambiguous envelopes).
        // 3: handshake carries a JWT access token; identity comes from the Auth API, not the device id.
        public const uint Current = 3;
        public const uint MinSupported = 3;
    }
}
