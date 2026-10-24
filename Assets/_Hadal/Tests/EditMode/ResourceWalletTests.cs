using HADAL.Shared.Enums;
using Hadal.Data.Models;
using NUnit.Framework;

namespace Hadal.Tests.EditMode
{
    /// <summary>
    /// QA R10 / S4 and the §5 #6 atomicity check: the optimistic client wallet must never go
    /// negative and a failed spend must leave every balance untouched.
    /// </summary>
    public sealed class ResourceWalletTests
    {
        [Test]
        public void Add_NeverGoesNegative()
        {
            var wallet = new ResourceWallet();
            wallet.Set(ResourceType.Food, 100);

            wallet.Add(ResourceType.Food, -1000);

            Assert.AreEqual(0, wallet.Get(ResourceType.Food));
        }

        [Test]
        public void Set_ClampsAtZero()
        {
            var wallet = new ResourceWallet();
            wallet.Set(ResourceType.Food, -5);
            Assert.AreEqual(0, wallet.Get(ResourceType.Food));
        }

        [Test]
        public void TrySpend_InsufficientFunds_ChangesNothing()
        {
            var wallet = new ResourceWallet();
            wallet.Set(ResourceType.Titanium, 100);
            wallet.Set(ResourceType.Energy, 5);

            var spent = wallet.TrySpend(new[]
            {
                new ResourceAmount(ResourceType.Titanium, 50),
                new ResourceAmount(ResourceType.Energy, 10)
            });

            Assert.IsFalse(spent);
            Assert.AreEqual(100, wallet.Get(ResourceType.Titanium));
            Assert.AreEqual(5, wallet.Get(ResourceType.Energy));
        }

        [Test]
        public void TrySpend_SameResourceListedTwice_ChecksTheSum()
        {
            // Each line alone is affordable (60 <= 100) but together they are not (120 > 100).
            var wallet = new ResourceWallet();
            wallet.Set(ResourceType.Titanium, 100);

            var spent = wallet.TrySpend(new[]
            {
                new ResourceAmount(ResourceType.Titanium, 60),
                new ResourceAmount(ResourceType.Titanium, 60)
            });

            Assert.IsFalse(spent);
            Assert.AreEqual(100, wallet.Get(ResourceType.Titanium));
        }

        [Test]
        public void TrySpend_Success_DeductsEveryLine()
        {
            var wallet = new ResourceWallet();
            wallet.Set(ResourceType.Titanium, 100);

            Assert.IsTrue(wallet.TrySpend(new[]
            {
                new ResourceAmount(ResourceType.Titanium, 30),
                new ResourceAmount(ResourceType.Titanium, 20)
            }));
            Assert.AreEqual(50, wallet.Get(ResourceType.Titanium));
        }
    }
}
