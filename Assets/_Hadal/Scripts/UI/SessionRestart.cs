using System;
using UnityEngine.SceneManagement;

namespace Hadal.UI
{
    /// <summary>
    /// Restarts the game on a new session (after signing in to another account) by reloading the
    /// bootstrap scene, which rebuilds the DI scope, socket and state from scratch.
    /// </summary>
    public static class SessionRestart
    {
        /// <summary>Replaced by editor play-mode tests, where the test runner drops the build index.</summary>
        public static Action Handler = () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        public static void Restart() => Handler();
    }
}
