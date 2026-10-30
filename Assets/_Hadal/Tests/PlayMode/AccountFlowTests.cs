#if UNITY_EDITOR
using System;
using System.Collections;
using HADAL.Shared.Auth;
using Hadal.Core.Network;
using Hadal.Managers.Bootstrap;
using Hadal.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VContainer;

namespace Hadal.Tests.PlayMode
{
    /// <summary>
    /// The account flow driven through the real UI (buttons and input fields) against a gateway:
    /// title screen → rename → protect with a transfer code → "new device" → wrong password →
    /// sign in with the code → back on the first account. Run by <c>unity.sh account-e2e</c>, which
    /// starts a throwaway in-memory gateway; skipped when HADAL_GATEWAY_URL is not set.
    /// </summary>
    public sealed class AccountFlowTests
    {
        private const string ScenePath = "Assets/_Hadal/Scenes/Bootstrap.unity";

        private string _profile;
        private Action _restart;

        [SetUp]
        public void RequireGateway()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HADAL_GATEWAY_URL")))
                Assert.Ignore("HADAL_GATEWAY_URL not set — account flow test skipped.");

            Environment.SetEnvironmentVariable("HADAL_LOCAL_SIM", "0");
            _profile = Environment.GetEnvironmentVariable("HADAL_AUTH_PROFILE");
            _restart = SessionRestart.Handler;
            SessionRestart.Handler = () => EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        }

        [TearDown]
        public void Restore()
        {
            Environment.SetEnvironmentVariable("HADAL_AUTH_PROFILE", _profile);
            Environment.SetEnvironmentVariable("HADAL_FRESH_ACCOUNT", null);
            if (_restart != null)
                SessionRestart.Handler = _restart;
        }

        [UnityTest, Category("AccountFlow")]
        public IEnumerator Protect_Then_SignInOnAnotherDevice_ThroughTheUi()
        {
            // Generated test values only.
            var password = "e2e-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            var deviceA = "account-e2e-a-" + Guid.NewGuid().ToString("N");
            var deviceB = "account-e2e-b-" + Guid.NewGuid().ToString("N");

            // --- Device A: first launch, title screen, profile -------------------------------------
            Environment.SetEnvironmentVariable("HADAL_AUTH_PROFILE", deviceA);
            Environment.SetEnvironmentVariable("HADAL_FRESH_ACCOUNT", "1");
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));

            var ui = UnityEngine.Object.FindFirstObjectByType<AccountUiRoot>();
            Assert.IsNotNull(ui, "AccountUiRoot missing from the Bootstrap scene");
            yield return WaitUntil(() => ui.Title != null && ui.Title.Current == TitleScreenView.Status.Ready, 20f, "title never became ready");

            Click(Find(ui.Title.transform, "Backdrop"));
            Assert.IsTrue(ui.Title.IsDismissed, "tap did not dismiss the title");

            var account = Container().Resolve<IAccountService>();
            yield return WaitUntil(() => account.Profile != null, 10f, "profile never loaded");
            var accountA = account.Profile.accountId;
            StringAssert.IsMatch("^Diver-[0-9]{4}$", account.Profile.displayName);
            Assert.IsFalse(account.IsProtected);
            Assert.IsTrue(Find(ui.Panel.transform, "Gear/Badge").gameObject.activeSelf, "unprotected account shows no badge");

            // --- Rename ------------------------------------------------------------------------------
            Click(Find(ui.Panel.transform, "Gear"));
            Assert.IsTrue(ui.Panel.IsOpen);
            Click(Find(ui.Panel.transform, "Panel/Card/Home/Row0/Account"));
            Type(ui.Panel.transform, "Panel/Card/Main/NameRow/Name", "E2E Kaptan");
            Click(Find(ui.Panel.transform, "Panel/Card/Main/NameRow/Save"));
            yield return WaitUntil(() => account.Profile.displayName == "E2E Kaptan", 10f, "rename not confirmed by the server");

            // --- Protect: mismatched passwords are caught locally, then a code is issued ------------
            Click(Find(ui.Panel.transform, "Panel/Card/Main/Protect"));
            Type(ui.Panel.transform, "Panel/Card/Protect/Password", password);
            Type(ui.Panel.transform, "Panel/Card/Protect/Password2", password + "x");
            Click(Find(ui.Panel.transform, "Panel/Card/Protect/Create"));
            Assert.AreEqual(Loc.Error("PasswordMismatch"), Text(ui.Panel.transform, "Panel/Card/Protect/Message"));

            Type(ui.Panel.transform, "Panel/Card/Protect/Password2", password);
            Click(Find(ui.Panel.transform, "Panel/Card/Protect/Create"));
            yield return WaitUntil(() => Text(ui.Panel.transform, "Panel/Card/Protect/Code").Length > 0, 10f, "no transfer code shown");
            var code = Text(ui.Panel.transform, "Panel/Card/Protect/Code");
            StringAssert.IsMatch("^[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}$", code);
            yield return WaitUntil(() => account.IsProtected, 10f, "account not marked protected");
            Assert.IsFalse(Find(ui.Panel.transform, "Gear/Badge").gameObject.activeSelf, "badge still shown after protecting");

            // --- Device B: a fresh install signs in as a new guest --------------------------------
            Environment.SetEnvironmentVariable("HADAL_AUTH_PROFILE", deviceB);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            ui = UnityEngine.Object.FindFirstObjectByType<AccountUiRoot>();
            account = Container().Resolve<IAccountService>();
            yield return WaitUntil(() => ui.Title.Current == TitleScreenView.Status.Ready, 20f, "device B title never ready");
            yield return WaitUntil(() => account.Profile != null, 10f, "device B profile never loaded");
            Assert.AreNotEqual(accountA, account.Profile.accountId, "device B should start as a different guest");

            // The stored session of device B must survive the reload that follows sign-in.
            Environment.SetEnvironmentVariable("HADAL_FRESH_ACCOUNT", null);

            Click(Find(ui.Panel.transform, "Gear"));
            Click(Find(ui.Panel.transform, "Panel/Card/Home/Row0/Account"));
            Click(Find(ui.Panel.transform, "Panel/Card/Main/Switch"));

            // Wrong password: an error, no restart.
            Type(ui.Panel.transform, "Panel/Card/Switch/Code", code);
            Type(ui.Panel.transform, "Panel/Card/Switch/Password", password + "-wrong");
            Click(Find(ui.Panel.transform, "Panel/Card/Switch/SignIn"));
            yield return WaitUntil(() => Text(ui.Panel.transform, "Panel/Card/Switch/Message").Length > 0, 10f, "no error for a wrong password");
            Assert.AreEqual(Loc.Error(AccountErrors.InvalidCredentials), Text(ui.Panel.transform, "Panel/Card/Switch/Message"));

            // Right password, code typed the lazy way: lower case, no dashes.
            var before = ui;
            Type(ui.Panel.transform, "Panel/Card/Switch/Code", code.Replace("-", string.Empty).ToLowerInvariant());
            Type(ui.Panel.transform, "Panel/Card/Switch/Password", password);
            Click(Find(ui.Panel.transform, "Panel/Card/Switch/SignIn"));

            // The scene restarts on the restored session.
            yield return WaitUntil(() => before == null, 10f, "scene did not restart after sign-in");
            yield return WaitUntil(() => UnityEngine.Object.FindFirstObjectByType<AccountUiRoot>() != null, 10f, "no UI after restart");
            ui = UnityEngine.Object.FindFirstObjectByType<AccountUiRoot>();
            account = Container().Resolve<IAccountService>();
            yield return WaitUntil(() => ui.Title.Current == TitleScreenView.Status.Ready, 20f, "restored session never loaded");
            yield return WaitUntil(() => account.Profile != null, 10f, "restored profile never loaded");

            Assert.AreEqual(accountA, account.Profile.accountId, "sign-in did not restore the first account");
            Assert.AreEqual("E2E Kaptan", account.Profile.displayName);
            Assert.IsTrue(account.IsProtected);
        }

        private static IObjectResolver Container()
        {
            var scope = UnityEngine.Object.FindFirstObjectByType<ProjectLifetimeScope>();
            Assert.IsNotNull(scope?.Container, "DI container was not built.");
            return scope.Container;
        }

        private static Transform Find(Transform root, string path)
        {
            var found = root.Find(path);
            Assert.IsNotNull(found, $"UI element '{path}' not found under {root.name}");
            return found;
        }

        private static void Click(Transform target)
        {
            var button = target.GetComponent<Button>();
            Assert.IsNotNull(button, $"{target.name} is not a button");
            Assert.IsTrue(button.interactable && button.gameObject.activeInHierarchy, $"{target.name} is not clickable");
            button.onClick.Invoke();
        }

        private static void Type(Transform root, string path, string value)
        {
            var field = Find(root, path).GetComponent<TMP_InputField>();
            Assert.IsNotNull(field, $"{path} is not an input field");
            field.text = value;
        }

        private static string Text(Transform root, string path)
        {
            var target = Find(root, path);
            return target.gameObject.activeInHierarchy ? target.GetComponent<TMP_Text>().text : string.Empty;
        }

        private static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds, string failure)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail(failure);
                yield return null;
            }
        }
    }
}
#endif
