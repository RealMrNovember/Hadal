using System;
using System.Threading.Tasks;
using HADAL.Shared.Auth;
using Hadal.Data.Config;
using UnityEngine;

namespace Hadal.Core.Network
{
    public interface IAuthService
    {
        /// <summary>A valid access token for the gateway handshake; refreshes or signs in as needed.</summary>
        Task<string> GetAccessTokenAsync();

        /// <summary>Drop the cached access token (e.g. the gateway said it expired).</summary>
        void InvalidateAccessToken();

        string AccountId { get; }
    }

    /// <summary>Where the refresh token lives between sessions.</summary>
    public interface IRefreshTokenStore
    {
        string Load();
        void Save(string refreshToken);
        void Clear();
    }

    /// <summary>
    /// PlayerPrefs store, one slot per auth profile. Plain text in the app's private storage: a
    /// stolen refresh token is single-use and rotated, and protected accounts can always be
    /// recovered with their transfer code. Move to Keychain/Keystore before soft launch (Phase 17).
    /// </summary>
    public sealed class PlayerPrefsRefreshTokenStore : IRefreshTokenStore
    {
        private readonly string _key;

        public PlayerPrefsRefreshTokenStore(GameConfigSO config)
        {
            var profile = config != null ? config.AuthProfile : null;
            _key = "hadal.auth." + (string.IsNullOrEmpty(profile) ? "default" : profile) + ".refresh";
        }

        public string Load() => PlayerPrefs.GetString(_key, string.Empty);

        public void Save(string refreshToken)
        {
            PlayerPrefs.SetString(_key, refreshToken);
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(_key);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Guest accounts: first launch creates one, later launches exchange the stored refresh token
    /// (single-use, rotated by the server) for a short-lived access token.
    /// </summary>
    public sealed class AuthService : IAuthService
    {
        private const int RenewBeforeExpirySeconds = 60;

        private readonly GameConfigSO _config;
        private readonly IRefreshTokenStore _store;

        private string _accessToken = string.Empty;
        private float _accessTokenExpiresAt;
        private Task<string> _inFlight;

        public AuthService(GameConfigSO config, IRefreshTokenStore store)
        {
            _config = config;
            _store = store;

            if (config != null && config.FreshAccount)
                _store.Clear();
        }

        public string AccountId { get; private set; } = string.Empty;

        public Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && Time.realtimeSinceStartup < _accessTokenExpiresAt)
                return Task.FromResult(_accessToken);

            // Concurrent callers share one request: a second refresh with the same token would
            // fail, because refresh tokens are single-use.
            return _inFlight ??= AcquireAsync();
        }

        public void InvalidateAccessToken()
        {
            _accessToken = string.Empty;
            _accessTokenExpiresAt = 0f;
        }

        private async Task<string> AcquireAsync()
        {
            try
            {
                var response = await RefreshAsync() ?? await GuestAsync();

                _store.Save(response.refreshToken);
                AccountId = response.accountId;
                _accessToken = response.accessToken;
                _accessTokenExpiresAt = Time.realtimeSinceStartup + Math.Max(0, response.expiresIn - RenewBeforeExpirySeconds);
                return _accessToken;
            }
            finally
            {
                _inFlight = null;
            }
        }

        private async Task<AuthResponse> RefreshAsync()
        {
            var refreshToken = _store.Load();
            if (string.IsNullOrEmpty(refreshToken))
                return null;

            var (status, body) = await HttpJson.PostAsync<RefreshRequest, AuthResponse>(
                _config.ApiBaseUrl + AuthRoutes.Refresh, new RefreshRequest { refreshToken = refreshToken });

            if (body != null)
                return body;

            if (status == 401)
            {
                // The account behind this token is unreachable from this device now; a protected
                // account comes back with its transfer code (Settings → Account).
                Debug.LogError("[AuthService] Stored refresh token rejected — starting a new guest account.");
                _store.Clear();
                return null;
            }

            throw new ServerUnreachableException($"Token refresh failed with HTTP {status}.");
        }

        private async Task<AuthResponse> GuestAsync()
        {
            var (status, body) = await HttpJson.PostAsync<GuestLoginRequest, AuthResponse>(
                _config.ApiBaseUrl + AuthRoutes.Guest,
                new GuestLoginRequest { deviceId = SystemInfo.deviceUniqueIdentifier, clientVersion = Application.version });

            if (body == null)
                throw new ServerUnreachableException($"Guest login failed with HTTP {status}.");

            Debug.Log($"[AuthService] Signed in as new guest account {body.accountId}.");
            return body;
        }
    }
}
