using System;

namespace HADAL.Shared.Auth
{
    // Auth API wire models (JSON over HTTPS — docs/17 allows JSON for auth only).
    // Public fields so Unity's JsonUtility and System.Text.Json (IncludeFields) both read them.

    public static class AuthRoutes
    {
        public const string Guest = "/api/v1/auth/guest";
        public const string Refresh = "/api/v1/auth/refresh";
        public const string Manifest = "/manifest/version.json";
    }

    /// <summary>Values of <c>HandshakeAckEnvelope.Reason</c>.</summary>
    public static class HandshakeRejectReasons
    {
        public const string SchemaIncompatible = "SchemaIncompatible";
        public const string TokenMissing = "TokenMissing";
        public const string TokenInvalid = "TokenInvalid";

        /// <summary>The client should refresh its access token and reconnect.</summary>
        public const string TokenExpired = "TokenExpired";
    }

    [Serializable]
    public sealed class GuestLoginRequest
    {
        /// <summary>Informational only (support, analytics). Never an identity.</summary>
        public string deviceId = string.Empty;
        public string clientVersion = string.Empty;
    }

    [Serializable]
    public sealed class RefreshRequest
    {
        public string refreshToken = string.Empty;
    }

    [Serializable]
    public sealed class AuthResponse
    {
        public string accountId = string.Empty;
        public string accessToken = string.Empty;

        /// <summary>Seconds until <see cref="accessToken"/> expires.</summary>
        public int expiresIn;

        /// <summary>Rotated on every refresh; the previous one stops working.</summary>
        public string refreshToken = string.Empty;
    }

    [Serializable]
    public sealed class VersionManifest
    {
        public string minClientVersion = "0.0.0";
        public string latestClientVersion = "0.0.0";
        public uint schemaVersion;
        public bool maintenance;
        public string message = string.Empty;
    }
}
