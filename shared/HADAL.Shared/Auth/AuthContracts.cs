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

        /// <summary>GET / PUT (Bearer access token): the signed-in account's profile.</summary>
        public const string Profile = "/api/v1/profile";

        /// <summary>POST (Bearer): set a password and get a transfer code that signs this account in elsewhere.</summary>
        public const string TransferCreate = "/api/v1/account/transfer";

        /// <summary>POST: sign in with a transfer code + password (new device, reinstall).</summary>
        public const string TransferLogin = "/api/v1/auth/transfer";

        /// <summary>POST (Bearer): link a Google / Apple identity to this account.</summary>
        public const string ProviderLink = "/api/v1/account/link";

        /// <summary>POST: sign in with a linked Google / Apple identity.</summary>
        public const string ProviderLogin = "/api/v1/auth/provider";

        /// <summary>POST (Bearer): redeem a gift code.</summary>
        public const string CouponRedeem = "/api/v1/coupons/redeem";

        /// <summary>GET: the live game catalog JSON (balance edited in the Overseer); header X-Catalog-Version.</summary>
        public const string Catalog = "/api/v1/catalog";
    }

    /// <summary>Identity providers an account can be linked to.</summary>
    public static class AccountProviders
    {
        public const string Transfer = "transfer";
        public const string Google = "google";
        public const string Apple = "apple";
    }

    /// <summary>Values of <see cref="AccountError.error"/> (HTTP 4xx/5xx bodies of the account API).</summary>
    public static class AccountErrors
    {
        public const string NameInvalid = "NameInvalid";
        public const string NameTaken = "NameTaken";
        public const string RenameCooldown = "RenameCooldown";
        public const string NeedRenameCard = "NeedRenameCard";
        public const string PasswordInvalid = "PasswordInvalid";
        public const string InvalidCredentials = "InvalidCredentials";
        public const string ProviderUnknown = "ProviderUnknown";
        public const string ProviderNotConfigured = "ProviderNotConfigured";
        public const string ProviderTokenInvalid = "ProviderTokenInvalid";
        public const string ProviderLinkedElsewhere = "ProviderLinkedElsewhere";
        public const string ProviderNotLinked = "ProviderNotLinked";
        public const string Banned = "Banned";
        public const string CouponUnknown = "CouponUnknown";
        public const string CouponInactive = "CouponInactive";
        public const string CouponExpired = "CouponExpired";
        public const string CouponExhausted = "CouponExhausted";
        public const string CouponAlreadyRedeemed = "CouponAlreadyRedeemed";
        public const string CouponNotStarted = "CouponNotStarted";
        public const string CouponNotForYou = "CouponNotForYou";
    }

    /// <summary>Display-name and password rules, shared so the client validates before sending.</summary>
    public static class AccountRules
    {
        public const int NameMinLength = 3;
        public const int NameMaxLength = 16;
        public const int PasswordMinLength = 8;
        public const int PasswordMaxLength = 64;

        /// <summary>Letters (any script), digits, space, '_' and '-'; no leading/trailing space.</summary>
        public static bool IsValidName(string? name)
        {
            if (name == null || name.Length < NameMinLength || name.Length > NameMaxLength || name.Trim() != name)
                return false;

            foreach (var c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != ' ' && c != '_' && c != '-')
                    return false;
            }

            return true;
        }

        public static bool IsValidPassword(string? password) =>
            password != null && password.Length >= PasswordMinLength && password.Length <= PasswordMaxLength;

        /// <summary>Transfer codes are shown as XXXX-XXXX-XXXX; input is accepted with or without dashes.</summary>
        public static string NormaliseTransferCode(string? code) =>
            (code ?? string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
    }

    /// <summary>Values of <c>HandshakeAckEnvelope.Reason</c>.</summary>
    public static class HandshakeRejectReasons
    {
        public const string SchemaIncompatible = "SchemaIncompatible";
        public const string TokenMissing = "TokenMissing";
        public const string TokenInvalid = "TokenInvalid";

        /// <summary>The client should refresh its access token and reconnect.</summary>
        public const string TokenExpired = "TokenExpired";

        /// <summary>The account is suspended; the client stops reconnecting.</summary>
        public const string Banned = "Banned";
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
    public sealed class ProfileResponse
    {
        public string accountId = string.Empty;
        public string displayName = string.Empty;

        /// <summary>ISO-8601 UTC.</summary>
        public string createdAt = string.Empty;

        /// <summary>Linked identities (<see cref="AccountProviders"/>); empty for an unprotected guest.</summary>
        public string[] linkedProviders = Array.Empty<string>();
    }

    [Serializable]
    public sealed class UpdateProfileRequest
    {
        public string displayName = string.Empty;
    }

    [Serializable]
    public sealed class TransferCreateRequest
    {
        public string password = string.Empty;
    }

    [Serializable]
    public sealed class TransferCodeResponse
    {
        /// <summary>XXXX-XXXX-XXXX. Creating a new one replaces the previous code.</summary>
        public string transferCode = string.Empty;
    }

    [Serializable]
    public sealed class TransferLoginRequest
    {
        public string transferCode = string.Empty;
        public string password = string.Empty;
        public string clientVersion = string.Empty;
    }

    [Serializable]
    public sealed class ProviderRequest
    {
        public string provider = string.Empty;

        /// <summary>OpenID Connect ID token from Sign in with Google / Sign in with Apple.</summary>
        public string idToken = string.Empty;
    }

    [Serializable]
    public sealed class CouponRedeemRequest
    {
        public string code = string.Empty;
    }

    [Serializable]
    public sealed class CouponRewardEntry
    {
        public string resource = string.Empty;
        public long amount;
    }

    [Serializable]
    public sealed class CouponRedeemResponse
    {
        public string code = string.Empty;
        public CouponRewardEntry[] rewards = Array.Empty<CouponRewardEntry>();
    }

    [Serializable]
    public sealed class AccountError
    {
        public string error = string.Empty;
    }

    [Serializable]
    public sealed class VersionManifest
    {
        public string minClientVersion = "0.0.0";
        public string latestClientVersion = "0.0.0";
        public uint schemaVersion;
        public bool maintenance;
        public string message = string.Empty;

        /// <summary>Overseer announcement shown on the title screen; empty = none.</summary>
        public string announcement = string.Empty;

        /// <summary>Live catalog version; the client downloads <see cref="AuthRoutes.Catalog"/> when it differs.</summary>
        public int catalogVersion;

        /// <summary>Download for <see cref="latestClientVersion"/> (Android APK; the store page once published).</summary>
        public string updateUrl = string.Empty;
    }
}
