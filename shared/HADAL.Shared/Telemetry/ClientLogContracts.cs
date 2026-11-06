using System;

namespace HADAL.Shared.Telemetry
{
    // Client crash/error reports. Public fields so Unity's JsonUtility and System.Text.Json (IncludeFields) both read them.
    public static class TelemetryRoutes
    {
        /// <summary>POST (Bearer): one batch of client-side errors/exceptions.</summary>
        public const string ClientLog = "/api/v1/telemetry/client-log";
    }

    public static class ClientLogLevels
    {
        public const string Error = "error";
        public const string Exception = "exception";
    }

    [Serializable]
    public sealed class ClientLogEntry
    {
        public string level = string.Empty;
        public string message = string.Empty;
        public string stack = string.Empty;
        /// <summary>Unix seconds on the client clock.</summary>
        public long at;
    }

    [Serializable]
    public sealed class ClientLogBatch
    {
        public ClientLogEntry[] entries = Array.Empty<ClientLogEntry>();
        public string clientVersion = string.Empty;
        public string platform = string.Empty;
        public string device = string.Empty;
    }

    [Serializable]
    public sealed class ClientLogAck
    {
        public int accepted;
    }
}
