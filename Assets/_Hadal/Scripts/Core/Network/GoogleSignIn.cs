using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Hadal.Core.Network
{
    /// <summary>An ID token from Sign in with Google, or an error code (<see cref="GoogleSignIn"/> constants).</summary>
    public readonly struct GoogleSignInResult
    {
        public GoogleSignInResult(string idToken, string email, string error)
        {
            IdToken = idToken ?? string.Empty;
            Email = email ?? string.Empty;
            Error = error ?? string.Empty;
        }

        public string IdToken { get; }

        /// <summary>The Google account's address, for display only (the server trusts the token, not this).</summary>
        public string Email { get; }

        public string Error { get; }
        public bool IsSuccess => Error.Length == 0 && IdToken.Length > 0;
    }

    /// <summary>Shows the Google account picker and returns an ID token for the gateway.</summary>
    public interface IGoogleSignIn
    {
        bool IsSupported { get; }

        /// <param name="serverClientId">The WEB OAuth client id: the token's audience on the gateway.</param>
        Task<GoogleSignInResult> SignInAsync(string serverClientId);
    }

    /// <summary>
    /// Android: Credential Manager through HadalGoogleSignIn.java (Assets/Plugins/Android).
    /// Editor and other platforms report <see cref="Unsupported"/>.
    /// </summary>
    public sealed class GoogleSignIn : IGoogleSignIn
    {
        public const string Cancelled = "GoogleCancelled";
        public const string NoAccount = "GoogleNoAccount";
        public const string Failed = "GoogleFailed";
        public const string Unsupported = "GoogleUnsupported";
        public const string NotConfigured = "GoogleNotConfigured";

        public bool IsSupported =>
#if UNITY_ANDROID && !UNITY_EDITOR
            true;
#else
            false;
#endif

        public Task<GoogleSignInResult> SignInAsync(string serverClientId)
        {
            if (string.IsNullOrWhiteSpace(serverClientId))
                return Task.FromResult(new GoogleSignInResult(null, null, NotConfigured));

#if UNITY_ANDROID && !UNITY_EDITOR
            // Completed on the Android UI thread; the awaiting code resumes on Unity's main thread
            // through the captured synchronization context.
            var completion = new TaskCompletionSource<GoogleSignInResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                using var plugin = new AndroidJavaClass("com.cicibyte.hadal.HadalGoogleSignIn");
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                plugin.CallStatic("signIn", activity, serverClientId.Trim(), new Listener(completion));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GoogleSignIn] {exception.Message}");
                completion.TrySetResult(new GoogleSignInResult(null, null, Failed));
            }

            return completion.Task;
#else
            return Task.FromResult(new GoogleSignInResult(null, null, Unsupported));
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private sealed class Listener : AndroidJavaProxy
        {
            private readonly TaskCompletionSource<GoogleSignInResult> _completion;

            public Listener(TaskCompletionSource<GoogleSignInResult> completion)
                : base("com.cicibyte.hadal.HadalGoogleSignIn$Listener")
            {
                _completion = completion;
            }

            // Called from Java by name; kept from IL2CPP code stripping.
            [UnityEngine.Scripting.Preserve]
            public void onSuccess(string idToken, string email) =>
                _completion.TrySetResult(string.IsNullOrEmpty(idToken)
                    ? new GoogleSignInResult(null, null, Failed)
                    : new GoogleSignInResult(idToken, email, null));

            [UnityEngine.Scripting.Preserve]
            public void onError(string code, string message)
            {
                if (code != Cancelled)
                    Debug.LogWarning($"[GoogleSignIn] {code}: {message}");
                _completion.TrySetResult(new GoogleSignInResult(null, null, string.IsNullOrEmpty(code) ? Failed : code));
            }
        }
#endif
    }
}
