using System;
using System.Threading.Tasks;
using HADAL.Shared.Auth;
using Hadal.Core.Network;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Gear button (top right) and the Profile & Account panel: player name, protection status,
    /// "protect account" (transfer code), "sign in to another account" and the Google / Apple
    /// placeholders. A badge on the gear nudges unprotected players without blocking play.
    /// </summary>
    public sealed class AccountPanelView : MonoBehaviour
    {
        private IAccountService _account;

        private GameObject _badge;
        private CanvasGroup _panel;
        private RectTransform _main;
        private RectTransform _protect;
        private RectTransform _switch;

        private InputField _name;
        private Text _accountId;
        private Text _protection;
        private Text _mainMessage;

        private InputField _password;
        private InputField _password2;
        private Text _codeLabel;
        private Text _code;
        private Button _copy;
        private Text _protectMessage;

        private InputField _transferCode;
        private InputField _transferPassword;
        private Text _switchMessage;

        private bool _busy;

        public bool IsOpen => _panel != null && _panel.gameObject.activeSelf;

        public static AccountPanelView Create(Transform parent, IAccountService account)
        {
            var canvas = UiKit.Canvas("AccountUi", 50);
            canvas.transform.SetParent(parent, false);
            var view = canvas.gameObject.AddComponent<AccountPanelView>();
            view._account = account;
            view.Build(canvas.transform);
            account.ProfileChanged += view.Render;
            view.Render(account.Profile);
            return view;
        }

        private void OnDestroy()
        {
            if (_account != null)
                _account.ProfileChanged -= Render;
        }

        private void Build(Transform root)
        {
            // Gear button.
            var gear = UiKit.Button("Gear", root, "≡", Open, new Color(0.03f, 0.10f, 0.14f, 0.85f), 64);
            gear.GetComponentInChildren<Text>().color = UiKit.Accent;
            ((RectTransform)gear.transform).Place(0.86f, 0.935f, 0.985f, 0.995f);
            _badge = UiKit.Box("Badge", gear.transform, UiKit.Amber).gameObject;
            ((RectTransform)_badge.transform).Place(0.78f, 0.72f, 0.98f, 0.98f);

            // Modal panel.
            var panelImage = UiKit.Box("Panel", root, new Color(0f, 0.01f, 0.02f, 0.7f));
            panelImage.rectTransform.Place(0, 0, 1, 1);
            _panel = panelImage.gameObject.AddComponent<CanvasGroup>();
            var card = UiKit.Box("Card", panelImage.transform, UiKit.Panel);
            card.rectTransform.Place(0.05f, 0.12f, 0.95f, 0.9f);

            var header = UiKit.Label("Header", card.transform, Loc.T("account.title"), 56, UiKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            header.rectTransform.Place(0.06f, 0.90f, 0.94f, 0.98f);

            _main = UiKit.Column("Main", card.transform);
            _main.Place(0.06f, 0.03f, 0.94f, 0.89f);
            BuildMain(_main);

            _protect = UiKit.Column("Protect", card.transform);
            _protect.Place(0.06f, 0.03f, 0.94f, 0.89f);
            BuildProtect(_protect);

            _switch = UiKit.Column("Switch", card.transform);
            _switch.Place(0.06f, 0.03f, 0.94f, 0.89f);
            BuildSwitch(_switch);

            _panel.gameObject.SetActive(false);
        }

        private void BuildMain(Transform column)
        {
            UiKit.Label("NameLabel", column, Loc.T("account.name"), 34, UiKit.Muted).Row(44);
            var nameRow = UiKit.Rect("NameRow", column).Row(110);
            _name = UiKit.Input("Name", nameRow, Loc.T("account.name"), characterLimit: AccountRules.NameMaxLength);
            ((RectTransform)_name.transform).Place(0, 0, 0.68f, 1);
            var save = UiKit.Button("Save", nameRow, Loc.T("account.save"), () => _ = SaveName());
            ((RectTransform)save.transform).Place(0.71f, 0, 1, 1);

            _accountId = UiKit.Label("AccountId", column, string.Empty, 30, UiKit.Muted).Row(44);
            _protection = UiKit.Label("Protection", column, string.Empty, 36, UiKit.Text).Row(120);

            UiKit.Button("Protect", column, Loc.T("account.protect"), () => Show(_protect)).Row(110);
            UiKit.Button("Switch", column, Loc.T("account.switch"), () => Show(_switch), UiKit.Field).Row(110)
                .GetComponentInChildren<Text>().color = UiKit.Text;

            var google = UiKit.Button("Google", column, Loc.T("account.google"), null, UiKit.Field).Row(100);
            google.interactable = false;
            google.GetComponentInChildren<Text>().color = UiKit.Muted;
            var apple = UiKit.Button("Apple", column, Loc.T("account.apple"), null, UiKit.Field).Row(100);
            apple.interactable = false;
            apple.GetComponentInChildren<Text>().color = UiKit.Muted;

            _mainMessage = UiKit.Label("Message", column, string.Empty, 32, UiKit.Danger).Row(80);
            UiKit.Button("Close", column, Loc.T("account.close"), Close, UiKit.Field).Row(100)
                .GetComponentInChildren<Text>().color = UiKit.Text;
        }

        private void BuildProtect(Transform column)
        {
            UiKit.Label("Hint", column, Loc.T("account.protect.hint"), 34, UiKit.Muted).Row(130);
            _password = UiKit.Input("Password", column, Loc.T("account.password"), true, AccountRules.PasswordMaxLength).Row(110);
            _password2 = UiKit.Input("Password2", column, Loc.T("account.password2"), true, AccountRules.PasswordMaxLength).Row(110);
            UiKit.Button("Create", column, Loc.T("account.create"), () => _ = CreateCode()).Row(110);

            _codeLabel = UiKit.Label("CodeLabel", column, Loc.T("account.code"), 34, UiKit.Muted).Row(44);
            _code = UiKit.Label("Code", column, string.Empty, 64, UiKit.Amber, TextAnchor.MiddleCenter, FontStyle.Bold).Row(100);
            _copy = UiKit.Button("Copy", column, Loc.T("account.copy"), CopyCode, UiKit.Field).Row(90);
            _copy.GetComponentInChildren<Text>().color = UiKit.Text;

            _protectMessage = UiKit.Label("Message", column, string.Empty, 32, UiKit.Danger).Row(110);
            UiKit.Button("Back", column, Loc.T("account.back"), () => Show(_main), UiKit.Field).Row(100)
                .GetComponentInChildren<Text>().color = UiKit.Text;
        }

        private void BuildSwitch(Transform column)
        {
            UiKit.Label("Hint", column, Loc.T("account.switch.hint"), 34, UiKit.Muted).Row(130);
            _transferCode = UiKit.Input("Code", column, Loc.T("account.transfercode"), characterLimit: 16).Row(110);
            _transferCode.characterValidation = InputField.CharacterValidation.None;
            _transferPassword = UiKit.Input("Password", column, Loc.T("account.password"), true, AccountRules.PasswordMaxLength).Row(110);
            UiKit.Button("SignIn", column, Loc.T("account.signin"), () => _ = SignIn()).Row(110);
            _switchMessage = UiKit.Label("Message", column, string.Empty, 32, UiKit.Danger).Row(110);
            UiKit.Button("Back", column, Loc.T("account.back"), () => Show(_main), UiKit.Field).Row(100)
                .GetComponentInChildren<Text>().color = UiKit.Text;
        }

        private void Render(ProfileResponse profile)
        {
            var isProtected = _account.IsProtected;
            if (_badge != null)
                _badge.SetActive(profile != null && !isProtected);

            if (profile == null || _name == null)
                return;

            if (!_name.isFocused)
                _name.text = profile.displayName;
            _accountId.text = $"{Loc.T("account.id")}: {profile.accountId.Substring(0, Math.Min(8, profile.accountId.Length)).ToUpperInvariant()}";
            _protection.text = isProtected ? Loc.T("account.protected") : Loc.T("account.unprotected");
            _protection.color = isProtected ? UiKit.Accent : UiKit.Amber;
        }

        public void Open()
        {
            Show(_main);
            _mainMessage.text = string.Empty;
            _panel.gameObject.SetActive(true);
            _panel.alpha = 1f;
            _panel.blocksRaycasts = true;
            Render(_account.Profile);
            _ = _account.RefreshProfileAsync();
        }

        public void Close() => _panel.gameObject.SetActive(false);

        private void Show(RectTransform page)
        {
            _main.gameObject.SetActive(page == _main);
            _protect.gameObject.SetActive(page == _protect);
            _switch.gameObject.SetActive(page == _switch);

            if (page == _protect)
            {
                _password.text = _password2.text = string.Empty;
                _protectMessage.text = string.Empty;
                SetCodeVisible(false);
            }
            else if (page == _switch)
            {
                _transferCode.text = _transferPassword.text = string.Empty;
                _switchMessage.text = string.Empty;
            }
        }

        private void SetCodeVisible(bool visible)
        {
            _codeLabel.gameObject.SetActive(visible);
            _code.gameObject.SetActive(visible);
            _copy.gameObject.SetActive(visible);
        }

        private async Task SaveName()
        {
            if (!Begin())
                return;

            var result = await _account.SetDisplayNameAsync(_name.text);
            _mainMessage.color = result.IsSuccess ? UiKit.Accent : UiKit.Danger;
            _mainMessage.text = result.IsSuccess ? Loc.T("account.saved") : Loc.Error(result.Error);
            _busy = false;
        }

        private async Task CreateCode()
        {
            if (_password.text != _password2.text)
            {
                _protectMessage.text = Loc.Error("PasswordMismatch");
                return;
            }

            if (!Begin())
                return;

            var result = await _account.CreateTransferCodeAsync(_password.text);
            if (result.IsSuccess)
            {
                _code.text = result.Value.transferCode;
                SetCodeVisible(true);
                _protectMessage.color = UiKit.Muted;
                _protectMessage.text = Loc.T("account.code.note");
                _password.text = _password2.text = string.Empty;
            }
            else
            {
                _protectMessage.color = UiKit.Danger;
                _protectMessage.text = Loc.Error(result.Error);
            }

            _busy = false;
        }

        private void CopyCode()
        {
            GUIUtility.systemCopyBuffer = _code.text;
            _copy.GetComponentInChildren<Text>().text = Loc.T("account.copied");
        }

        private async Task SignIn()
        {
            if (!Begin())
                return;

            var result = await _account.SignInWithTransferAsync(_transferCode.text, _transferPassword.text);
            if (result.IsSuccess)
            {
                // Every system (state view, socket, crew...) restarts from the other account.
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            _switchMessage.text = Loc.Error(result.Error);
            _busy = false;
        }

        private bool Begin()
        {
            if (_busy)
                return false;

            _busy = true;
            return true;
        }
    }
}
