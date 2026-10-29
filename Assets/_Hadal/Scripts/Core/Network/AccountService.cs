using System;
using System.Threading.Tasks;
using HADAL.Shared.Auth;
using Hadal.Data.Config;
using UnityEngine;
using UnityEngine.Networking;

namespace Hadal.Core.Network
{
    /// <summary>Outcome of an account call: a value, or an <see cref="AccountErrors"/> / transport error code.</summary>
    public readonly struct AccountResult<T> where T : class
    {
        /// <summary>The server could not be reached.</summary>
        public const string NetworkError = "Network";

        public AccountResult(T value, string error)
        {
            Value = value;
            Error = error ?? string.Empty;
        }

        public T Value { get; }
        public string Error { get; }
        public bool IsSuccess => Value != null && Error.Length == 0;
    }

    /// <summary>Profile and account protection: transfer code and Sign in with Google (Apple comes with its SDK).</summary>
    public interface IAccountService
    {
        /// <summary>Last known profile; null until the first successful <see cref="RefreshProfileAsync"/>.</summary>
        ProfileResponse Profile { get; }

        /// <summary>True when the account can be recovered on another device (any linked identity).</summary>
        bool IsProtected { get; }

        event Action<ProfileResponse> ProfileChanged;

        Task<AccountResult<ProfileResponse>> RefreshProfileAsync();

        Task<AccountResult<ProfileResponse>> SetDisplayNameAsync(string displayName);

        Task<AccountResult<TransferCodeResponse>> CreateTransferCodeAsync(string password);

        /// <summary>
        /// Signs in to the account behind a transfer code. On success the new session is stored;
        /// the caller restarts the game so every system starts from that account's state.
        /// </summary>
        Task<AccountResult<AuthResponse>> SignInWithTransferAsync(string transferCode, string password);

        /// <summary>Redeems a gift code; the rewards reach the wallet through the next state update.</summary>
        Task<AccountResult<CouponRedeemResponse>> RedeemCouponAsync(string code);

        /// <summary>True when the platform can show the Google account picker and a web client id is configured.</summary>
        bool CanUseGoogle { get; }

        /// <summary>True when the last known profile has a Google identity linked.</summary>
        bool IsGoogleLinked { get; }

        /// <summary>Address of the Google account linked from this device (display only); empty when unknown.</summary>
        string GoogleEmail { get; }

        /// <summary>Google account picker, then links that identity to this account (protects it).</summary>
        Task<AccountResult<ProfileResponse>> LinkGoogleAsync();

        /// <summary>
        /// Google account picker, then signs in to the account that identity is linked to. On success the
        /// new session is stored; the caller restarts the game like after a transfer-code sign-in.
        /// </summary>
        Task<AccountResult<AuthResponse>> SignInWithGoogleAsync();
    }

    public sealed class AccountService : IAccountService
    {
        private readonly GameConfigSO _config;
        private readonly IAuthService _auth;
        private readonly IRefreshTokenStore _store;
        private readonly IGoogleSignIn _google;

        public AccountService(GameConfigSO config, IAuthService auth, IRefreshTokenStore store, IGoogleSignIn google)
        {
            _config = config;
            _auth = auth;
            _store = store;
            _google = google;
        }

        public ProfileResponse Profile { get; private set; }

        public bool IsProtected => Profile != null && Profile.linkedProviders != null && Profile.linkedProviders.Length > 0;

        public event Action<ProfileResponse> ProfileChanged;

        public async Task<AccountResult<ProfileResponse>> RefreshProfileAsync()
        {
            var result = await AuthorizedAsync<ProfileResponse>(UnityWebRequest.kHttpVerbGET, AuthRoutes.Profile, null);
            Remember(result);
            return result;
        }

        public async Task<AccountResult<ProfileResponse>> SetDisplayNameAsync(string displayName)
        {
            var name = (displayName ?? string.Empty).Trim();
            if (!AccountRules.IsValidName(name))
                return new AccountResult<ProfileResponse>(null, AccountErrors.NameInvalid);
            if (NameFilter.IsProfane(name))
                return new AccountResult<ProfileResponse>(null, AccountErrors.NameProfane);

            var result = await AuthorizedAsync<ProfileResponse>(UnityWebRequest.kHttpVerbPUT, AuthRoutes.Profile,
                new UpdateProfileRequest { displayName = name });
            Remember(result);
            return result;
        }

        public async Task<AccountResult<TransferCodeResponse>> CreateTransferCodeAsync(string password)
        {
            if (!AccountRules.IsValidPassword(password))
                return new AccountResult<TransferCodeResponse>(null, AccountErrors.PasswordInvalid);

            var result = await AuthorizedAsync<TransferCodeResponse>(UnityWebRequest.kHttpVerbPOST, AuthRoutes.TransferCreate,
                new TransferCreateRequest { password = password });
            if (result.IsSuccess)
                await RefreshProfileAsync(); // now protected
            return result;
        }

        public async Task<AccountResult<AuthResponse>> SignInWithTransferAsync(string transferCode, string password)
        {
            try
            {
                var response = await HttpJson.SendAsync<AuthResponse>(UnityWebRequest.kHttpVerbPOST, _config.ApiBaseUrl + AuthRoutes.TransferLogin,
                    new TransferLoginRequest { transferCode = transferCode, password = password, clientVersion = Application.version }, null);
                if (!response.IsSuccess || response.Body == null)
                    return new AccountResult<AuthResponse>(null, ErrorOf(response.Status, response.Error));

                _store.Save(response.Body.refreshToken);
                return new AccountResult<AuthResponse>(response.Body, null);
            }
            catch (ServerUnreachableException)
            {
                return new AccountResult<AuthResponse>(null, AccountResult<AuthResponse>.NetworkError);
            }
        }

        public Task<AccountResult<CouponRedeemResponse>> RedeemCouponAsync(string code)
        {
            var normalised = (code ?? string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty).Trim().ToUpperInvariant();
            if (normalised.Length == 0)
                return Task.FromResult(new AccountResult<CouponRedeemResponse>(null, AccountErrors.CouponUnknown));

            return AuthorizedAsync<CouponRedeemResponse>(UnityWebRequest.kHttpVerbPOST, AuthRoutes.CouponRedeem,
                new CouponRedeemRequest { code = normalised });
        }

        public bool CanUseGoogle => _google != null && _google.IsSupported && _config.GoogleWebClientId.Length > 0;

        public bool IsGoogleLinked => Profile?.linkedProviders != null && Array.IndexOf(Profile.linkedProviders, AccountProviders.Google) >= 0;

        public string GoogleEmail => Profile == null ? string.Empty : PlayerPrefs.GetString(GoogleEmailKey(Profile.accountId), string.Empty);

        public async Task<AccountResult<ProfileResponse>> LinkGoogleAsync()
        {
            var google = await PickGoogleAsync();
            if (!google.IsSuccess)
                return new AccountResult<ProfileResponse>(null, google.Error);

            var result = await AuthorizedAsync<ProfileResponse>(UnityWebRequest.kHttpVerbPOST, AuthRoutes.ProviderLink,
                new ProviderRequest { provider = AccountProviders.Google, idToken = google.IdToken });
            if (result.IsSuccess)
                RememberGoogleEmail(result.Value.accountId, google.Email);
            Remember(result);
            return result;
        }

        public async Task<AccountResult<AuthResponse>> SignInWithGoogleAsync()
        {
            var google = await PickGoogleAsync();
            if (!google.IsSuccess)
                return new AccountResult<AuthResponse>(null, google.Error);

            try
            {
                var response = await HttpJson.SendAsync<AuthResponse>(UnityWebRequest.kHttpVerbPOST, _config.ApiBaseUrl + AuthRoutes.ProviderLogin,
                    new ProviderRequest { provider = AccountProviders.Google, idToken = google.IdToken }, null);
                if (!response.IsSuccess || response.Body == null)
                    return new AccountResult<AuthResponse>(null, ErrorOf(response.Status, response.Error));

                _store.Save(response.Body.refreshToken);
                RememberGoogleEmail(response.Body.accountId, google.Email);
                return new AccountResult<AuthResponse>(response.Body, null);
            }
            catch (ServerUnreachableException)
            {
                return new AccountResult<AuthResponse>(null, AccountResult<AuthResponse>.NetworkError);
            }
        }

        private Task<GoogleSignInResult> PickGoogleAsync()
        {
            if (_google == null || !_google.IsSupported)
                return Task.FromResult(new GoogleSignInResult(null, null, GoogleSignIn.Unsupported));
            return _google.SignInAsync(_config.GoogleWebClientId);
        }

        private static string GoogleEmailKey(string accountId) => "hadal.google.email." + accountId;

        private static void RememberGoogleEmail(string accountId, string email)
        {
            if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(email))
                return;
            PlayerPrefs.SetString(GoogleEmailKey(accountId), email);
            PlayerPrefs.Save();
        }

        private async Task<AccountResult<T>> AuthorizedAsync<T>(string method, string route, object body) where T : class
        {
            try
            {
                var token = await _auth.GetAccessTokenAsync();
                var response = await HttpJson.SendAsync<T>(method, _config.ApiBaseUrl + route, body, token);
                if (response.Status == 401 && string.IsNullOrEmpty(response.Error))
                {
                    // Access token expired between checks: renew once and retry.
                    _auth.InvalidateAccessToken();
                    token = await _auth.GetAccessTokenAsync();
                    response = await HttpJson.SendAsync<T>(method, _config.ApiBaseUrl + route, body, token);
                }

                return response.IsSuccess && response.Body != null
                    ? new AccountResult<T>(response.Body, null)
                    : new AccountResult<T>(null, ErrorOf(response.Status, response.Error));
            }
            catch (ServerUnreachableException)
            {
                return new AccountResult<T>(null, AccountResult<T>.NetworkError);
            }
        }

        private void Remember(AccountResult<ProfileResponse> result)
        {
            if (!result.IsSuccess)
                return;

            Profile = result.Value;
            ProfileChanged?.Invoke(Profile);
        }

        private static string ErrorOf(long status, string error) =>
            !string.IsNullOrEmpty(error) ? error : status == 429 ? "RateLimited" : "Http" + status;
    }
}
