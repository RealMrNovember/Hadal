#if HADAL_FCM && UNITY_ANDROID && !UNITY_EDITOR
using System;

namespace Hadal.UI
{
    /// <summary>
    /// Phase 2 seam (docs/36 §5): the phone's Firebase Cloud Messaging token. Compiled only with the HADAL_FCM scripting
    /// define, which is set once the Firebase Messaging Unity SDK and google-services.json are in the project; without
    /// them the game has no token source and never registers for push.
    /// </summary>
    internal sealed class FirebasePushTokenSource : IPushTokenSource
    {
        public string Token { get; private set; }

        public event Action<string> TokenChanged;

        public FirebasePushTokenSource()
        {
            Firebase.Messaging.FirebaseMessaging.TokenReceived += (_, e) =>
            {
                Token = e.Token;
                TokenChanged?.Invoke(e.Token);
            };
            Firebase.Extensions.TaskExtension.ContinueWithOnMainThread(Firebase.Messaging.FirebaseMessaging.GetTokenAsync(), task =>
            {
                if (task.IsCompleted && !task.IsFaulted && !string.IsNullOrEmpty(task.Result))
                {
                    Token = task.Result;
                    TokenChanged?.Invoke(task.Result);
                }
            });
        }
    }
}
#endif
