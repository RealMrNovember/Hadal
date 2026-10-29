using System;
using System.Threading.Tasks;
using HADAL.Shared.Auth;
using Hadal.Core.Network;
using Hadal.UI;
using NUnit.Framework;
using UnityEngine;

namespace Hadal.Tests.EditMode
{
    /// <summary>The gift-code page tells every refusal apart (docs/22: coupons).</summary>
    public sealed class GiftCodeTests
    {
        private GameObject _root;
        private FakeAccount _account;
        private AccountPanelView _panel;
        private string _language;

        [SetUp]
        public void SetUp()
        {
            _language = Loc.Current;
            Loc.Use("tr");
            _root = new GameObject("GiftCodeTests");
            _account = new FakeAccount();
            _panel = AccountPanelView.Create(_root.transform, _account);
            _panel.OpenGift();
        }

        [TearDown]
        public void TearDown()
        {
            Loc.Use(_language);
            UnityEngine.Object.DestroyImmediate(_root);
        }

        [TestCase(AccountErrors.CouponAlreadyRedeemed, "Bu kodu zaten kullandın.")]
        [TestCase(AccountErrors.CouponExhausted, "Bu kodun kullanım limiti doldu.")]
        [TestCase(AccountErrors.CouponExpired, "Bu kodun süresi doldu.")]
        [TestCase(AccountErrors.CouponNotStarted, "Bu kod henüz aktif değil.")]
        [TestCase(AccountErrors.CouponNotForYou, "Bu kod sana ait değil.")]
        [TestCase(AccountErrors.CouponUnknown, "Böyle bir kod bulunamadı.")]
        [TestCase(AccountErrors.CouponInactive, "Bu kod artık geçerli değil.")]
        public void Each_refusal_has_its_own_message(string error, string expected)
        {
            _account.Next = new AccountResult<CouponRedeemResponse>(null, error);
            Redeem("HADAL2026");

            Assert.AreEqual("HADAL2026", _account.LastCode);
            Assert.AreEqual(expected, _panel.GiftMessage);
        }

        [Test]
        public void Success_lists_the_rewards()
        {
            _account.Next = new AccountResult<CouponRedeemResponse>(new CouponRedeemResponse
            {
                code = "HOSGELDIN",
                rewards = new[] { new CouponRewardEntry { resource = "Titanium", amount = 300 }, new CouponRewardEntry { resource = "Hadalite", amount = 5 } }
            }, null);
            Redeem("hosgeldin");

            Assert.AreEqual("Ödüller eklendi: +300 Titanyum, +5 Hadalit", _panel.GiftMessage);
        }

        // The fake answers synchronously, so the whole redeem runs inside this call.
        private void Redeem(string code)
        {
            var task = _panel.RedeemAsync(code);
            Assert.IsTrue(task.IsCompleted);
            task.GetAwaiter().GetResult();
        }

        private sealed class FakeAccount : IAccountService
        {
            public AccountResult<CouponRedeemResponse> Next;
            public string LastCode;

            public ProfileResponse Profile => null;
            public bool IsProtected => false;
            public event Action<ProfileResponse> ProfileChanged { add { } remove { } }

            public Task<AccountResult<CouponRedeemResponse>> RedeemCouponAsync(string code)
            {
                LastCode = code;
                return Task.FromResult(Next);
            }

            public Task<AccountResult<ProfileResponse>> RefreshProfileAsync() =>
                Task.FromResult(new AccountResult<ProfileResponse>(null, "Offline"));

            public Task<AccountResult<ProfileResponse>> SetDisplayNameAsync(string displayName) => throw new NotSupportedException();
            public Task<AccountResult<TransferCodeResponse>> CreateTransferCodeAsync(string password) => throw new NotSupportedException();
            public Task<AccountResult<AuthResponse>> SignInWithTransferAsync(string transferCode, string password) => throw new NotSupportedException();
            public bool CanUseGoogle => false;
            public bool IsGoogleLinked => false;
            public string GoogleEmail => string.Empty;
            public Task<AccountResult<ProfileResponse>> LinkGoogleAsync() => throw new NotSupportedException();
            public Task<AccountResult<AuthResponse>> SignInWithGoogleAsync() => throw new NotSupportedException();
        }
    }
}
