using System;
using System.Threading.Tasks;
using HADAL.Shared.Auth;
using HADAL.Shared.Simulation;
using Hadal.Core.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Gear button (top right) and the Settings dialog (Last Asylum's: a centred dialog over the blurred game, the home
    /// page a grid of icon tiles). Its tiles lead to Account (player name, protection status, transfer code, Link with
    /// Google, sign in elsewhere by transfer code or Google, Apple placeholder), the game settings (sound and graphics
    /// in sections with ON/OFF switches), Language, Notifications, Gift Code, Support and About (SettingsPages.cs).
    /// A badge on the gear nudges unprotected players without blocking play.
    /// </summary>
    public sealed partial class AccountPanelView : MonoBehaviour
    {
        /// <summary>Amber that still reads on the light body (unprotected account).</summary>
        private static readonly Color Warn = new(0.74f, 0.42f, 0.04f, 1f);

        private IAccountService _account;

        private GameObject _badge;
        private CanvasGroup _panel;
        private TMP_Text _header;
        private RectTransform _card;
        private readonly System.Collections.Generic.List<(RectTransform Page, string TitleKey)> _pages = new();
        private RectTransform _main;
        private RectTransform _protect;
        private RectTransform _switch;
        private RectTransform _gift;

        private TMP_InputField _name;
        private TMP_Text _accountId;
        private TMP_Text _protection;
        private TMP_Text _mainMessage;

        private TMP_InputField _password;
        private TMP_InputField _password2;
        private TMP_Text _codeLabel;
        private TMP_Text _code;
        private Button _copy;
        private TMP_Text _protectMessage;

        private TMP_InputField _transferCode;
        private TMP_InputField _transferPassword;
        private TMP_Text _switchMessage;

        private TMP_InputField _giftCode;
        private TMP_Text _giftMessage;

        private bool _busy;
        private GameObject _gear;

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
            gear.GetComponentInChildren<TMP_Text>().color = UiKit.Accent;
            _gear = gear.gameObject;
            ((RectTransform)gear.transform).Place(0.86f, 0.935f, 0.985f, 0.995f);
            _badge = UiKit.Box("Badge", gear.transform, UiKit.Amber).gameObject;
            ((RectTransform)_badge.transform).Place(0.78f, 0.72f, 0.98f, 0.98f);

            // Last Asylum's settings dialog over the blurred game: the white cross steps back a page, a tap outside closes.
            var dim = UiFrames.Dim(root, Close);
            dim.name = "Panel";
            _panel = dim.gameObject.AddComponent<CanvasGroup>();
            var card = UiKit.Box("Card", dim, UiFrames.Navy);
            _card = card.rectTransform;
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>().From = 0.94f;
            _header = UiFrames.Dialog(_card, Back).Title;
            _header.text = Loc.T("settings.title");

            BuildSettings(card.transform);
            BuildMain(_main = Page("Main", card.transform, "account.title"));
            BuildProtect(_protect = Page("Protect", card.transform, "account.title"));
            BuildSwitch(_switch = Page("Switch", card.transform, "account.title"));
            BuildGift(_gift = Page("Gift", card.transform, "settings.gift"));

            _panel.gameObject.SetActive(false);
        }

        /// <summary>A page: a column over the dialog's light body (direct child of the card, under the title).</summary>
        private RectTransform Page(string name, Transform card, string titleKey)
        {
            var page = UiKit.Column(name, card, 14f);
            page.anchorMin = Vector2.zero;
            page.anchorMax = Vector2.one;
            page.offsetMin = new Vector2(36f, 34f);
            page.offsetMax = new Vector2(-36f, -128f);
            _pages.Add((page, titleKey));
            return page;
        }

        /// <summary>The cross: an inner page goes back to where it was opened from; the home page closes.</summary>
        private void Back()
        {
            var shown = _pages.Find(p => p.Page.gameObject.activeSelf).Page;
            if (shown == null || shown == _home)
                Close();
            else if (shown == _protect || shown == _switch)
                Show(_main);
            else
                Show(_home);
        }

        private void BuildMain(Transform column)
        {
            UiFrames.Section(column, Loc.T("account.name")).Row(56);
            var nameRow = UiKit.Rect("NameRow", column).Row(110);
            _name = UiKit.Input("Name", nameRow, Loc.T("account.name"), characterLimit: AccountRules.NameMaxLength);
            ((RectTransform)_name.transform).Place(0, 0, 0.68f, 1);
            var save = UiKit.Button("Save", nameRow, Loc.T("account.save"), () => _ = SaveName(), UiKit.Primary, UiKit.TypeScale.Subtitle);
            ((RectTransform)save.transform).Place(0.71f, 0, 1, 1);
            UiKit.Label("NameRule", column, Loc.F("account.rename.rule", RenameRules.HadalitePrice), UiKit.TypeScale.Caption, UiFrames.InkSoft).Row(76);

            UiFrames.Section(column, Loc.T("settings.account")).Row(56);
            _accountId = UiKit.Label("AccountId", column, string.Empty, UiKit.TypeScale.Body, UiFrames.InkSoft).Row(44);
            _protection = UiKit.Label("Protection", column, string.Empty, UiKit.TypeScale.Body, UiFrames.Ink).Row(110);

            UiKit.Button("Protect", column, Loc.T("account.protect"), () => Show(_protect), UiKit.Primary, UiKit.TypeScale.Subtitle).Row(104);
            UiKit.Button("Switch", column, Loc.T("account.switch"), () => Show(_switch), UiKit.Field, UiKit.TypeScale.Subtitle).Row(104);
            UiKit.Button("Google", column, Loc.T("account.google"), () => _ = LinkGoogle(), UiKit.Field, UiKit.TypeScale.Subtitle).Row(96);
            var apple = UiKit.Button("Apple", column, Loc.T("account.apple"), null, UiKit.Panel, UiKit.TypeScale.Subtitle).Row(96);
            apple.interactable = false;

            _mainMessage = UiKit.Label("Message", column, string.Empty, UiKit.TypeScale.Body, UiFrames.InkBad, TextAnchor.MiddleCenter).Row(80);
        }

        private void BuildProtect(Transform column)
        {
            UiFrames.Note(column, Loc.T("account.protect.hint")).Row(150);
            _password = UiKit.Input("Password", column, Loc.T("account.password"), true, AccountRules.PasswordMaxLength).Row(110);
            _password2 = UiKit.Input("Password2", column, Loc.T("account.password2"), true, AccountRules.PasswordMaxLength).Row(110);
            UiKit.Button("Create", column, Loc.T("account.create"), () => _ = CreateCode(), UiKit.Primary, UiKit.TypeScale.Subtitle).Row(104);

            _codeLabel = UiKit.Label("CodeLabel", column, Loc.T("account.code"), UiKit.TypeScale.Body, UiFrames.InkSoft, TextAnchor.MiddleCenter).Row(44);
            _code = UiKit.Label("Code", column, string.Empty, UiKit.TypeScale.Display + 8, UiFrames.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).Row(100);
            _copy = UiKit.Button("Copy", column, Loc.T("account.copy"), CopyCode, UiKit.Field, UiKit.TypeScale.Subtitle).Row(90);

            _protectMessage = UiKit.Label("Message", column, string.Empty, UiKit.TypeScale.Body, UiFrames.InkBad, TextAnchor.MiddleCenter).Row(110);
        }

        private void BuildSwitch(Transform column)
        {
            UiFrames.Note(column, Loc.T("account.switch.hint")).Row(150);
            _transferCode = UiKit.Input("Code", column, Loc.T("account.transfercode"), characterLimit: 16).Row(110);
            _transferCode.characterValidation = TMP_InputField.CharacterValidation.None;
            _transferPassword = UiKit.Input("Password", column, Loc.T("account.password"), true, AccountRules.PasswordMaxLength).Row(110);
            UiKit.Button("SignIn", column, Loc.T("account.signin"), () => _ = SignIn(), UiKit.Primary, UiKit.TypeScale.Subtitle).Row(104);
            UiKit.Button("GoogleSignIn", column, Loc.T("account.google.signin"), () => _ = SignInWithGoogle(), UiKit.Field, UiKit.TypeScale.Subtitle).Row(104);
            _switchMessage = UiKit.Label("Message", column, string.Empty, UiKit.TypeScale.Body, UiFrames.InkBad, TextAnchor.MiddleCenter).Row(110);
        }

        private void BuildGift(Transform column)
        {
            UiFrames.Note(column, Loc.T("gift.hint")).Row(150);
            _giftCode = UiKit.Input("Code", column, Loc.T("gift.code"), characterLimit: 24).Row(110);
            _giftCode.characterValidation = TMP_InputField.CharacterValidation.None;
            UiKit.Button("Redeem", column, Loc.T("gift.redeem"), () => _ = Redeem(), UiKit.Gold, UiKit.TypeScale.Subtitle).Row(104);
            _giftMessage = UiKit.Label("Message", column, string.Empty, UiKit.TypeScale.Body + 2, UiFrames.InkBad, TextAnchor.MiddleCenter).Row(160);
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
            if (_account.IsGoogleLinked)
                _protection.text += "\n" + GoogleStatus();
            _protection.color = isProtected ? UiFrames.InkGood : Warn;
        }

        /// <summary>The main HUD opens settings from the avatar; the corner gear is then not needed.</summary>
        public void HideGear()
        {
            if (_gear != null)
                _gear.SetActive(false);
        }

        public void Open()
        {
            Show(_home);
            _mainMessage.text = string.Empty;
            _panel.gameObject.SetActive(true);
            _panel.alpha = 1f;
            _panel.blocksRaycasts = true;
            Render(_account.Profile);
            _ = _account.RefreshProfileAsync();
        }

        public void Close() => _panel.gameObject.SetActive(false);

        /// <summary>Opens the Account page (tests, the protection badge).</summary>
        public void OpenAccount()
        {
            Open();
            Show(_main);
        }

        private void Show(RectTransform page)
        {
            foreach (var (candidate, titleKey) in _pages)
            {
                candidate.gameObject.SetActive(candidate == page);
                if (candidate == page)
                    _header.text = Loc.T(titleKey);
            }

            // Content-sized like Last Asylum's: the tile grid in a short dialog, the inner pages taller.
            var share = page == _home ? 0.38f : 0.8f;
            _card.Place(0.03f, 0.5f - share / 2f, 0.97f, 0.5f + share / 2f);
            OnPageShown(page);

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
            else if (page == _gift)
            {
                _giftCode.text = string.Empty;
                _giftMessage.text = string.Empty;
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
            _mainMessage.color = result.IsSuccess ? UiFrames.InkGood : UiFrames.InkBad;
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
                _protectMessage.color = UiFrames.InkSoft;
                _protectMessage.text = Loc.T("account.code.note");
                _password.text = _password2.text = string.Empty;
            }
            else
            {
                _protectMessage.color = UiFrames.InkBad;
                _protectMessage.text = Loc.Error(result.Error);
            }

            _busy = false;
        }

        private void CopyCode()
        {
            GUIUtility.systemCopyBuffer = _code.text;
            _copy.GetComponentInChildren<TMP_Text>().text = Loc.T("account.copied");
        }

        private async Task SignIn()
        {
            if (!Begin())
                return;

            var result = await _account.SignInWithTransferAsync(_transferCode.text, _transferPassword.text);
            if (result.IsSuccess)
            {
                // Every system (state view, socket, crew...) restarts from the other account.
                SessionRestart.Restart();
                return;
            }

            _switchMessage.color = UiFrames.InkBad;
            _switchMessage.text = Loc.Error(result.Error);
            _busy = false;
        }

        private string GoogleStatus()
        {
            var email = _account.GoogleEmail;
            return string.IsNullOrEmpty(email) ? Loc.T("account.google.linked") : Loc.F("account.google.linked.email", email);
        }

        private async Task LinkGoogle()
        {
            if (!Begin())
                return;

            _mainMessage.color = UiFrames.InkSoft;
            _mainMessage.text = Loc.T("account.google.working");
            var result = await _account.LinkGoogleAsync();
            _mainMessage.color = result.IsSuccess ? UiFrames.InkGood : UiFrames.InkBad;
            _mainMessage.text = result.IsSuccess ? GoogleStatus() : Loc.Error(result.Error);
            _busy = false;
        }

        private async Task SignInWithGoogle()
        {
            if (!Begin())
                return;

            _switchMessage.color = UiFrames.InkSoft;
            _switchMessage.text = Loc.T("account.google.working");
            var result = await _account.SignInWithGoogleAsync();
            if (result.IsSuccess)
            {
                // Same as a transfer-code sign-in: every system restarts from the other account.
                SessionRestart.Restart();
                return;
            }

            _switchMessage.color = UiFrames.InkBad;
            _switchMessage.text = Loc.Error(result.Error);
            _busy = false;
        }

        /// <summary>Opens the gift-code page directly (tests, deep links).</summary>
        public void OpenGift()
        {
            Open();
            Show(_gift);
        }

        public string GiftMessage => _giftMessage.text;

        public async Task RedeemAsync(string code)
        {
            _giftCode.text = code;
            await Redeem();
        }

        private async Task Redeem()
        {
            if (!Begin())
                return;

            var result = await _account.RedeemCouponAsync(_giftCode.text);
            if (result.IsSuccess)
            {
                var rewards = string.Join(", ", Array.ConvertAll(result.Value.rewards ?? Array.Empty<CouponRewardEntry>(),
                    r => $"+{r.amount} {Loc.T("resource." + r.resource)}"));
                _giftMessage.color = UiFrames.InkGood;
                _giftMessage.text = Loc.F("gift.done", rewards);
                _giftCode.text = string.Empty;
            }
            else
            {
                _giftMessage.color = UiFrames.InkBad;
                _giftMessage.text = Loc.Error(result.Error);
            }

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
