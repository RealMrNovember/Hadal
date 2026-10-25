using Hadal.Gateway.Auth;
using HADAL.Shared.Auth;
using Microsoft.Extensions.Options;

namespace Hadal.Game.Tests
{
    public sealed class TokenServiceTests
    {
        private const string Secret = "unit-secret-unit-secret-unit-secret-0123";

        private readonly FakeClock _clock = new();
        private readonly InMemoryAccountStore _accounts = new();

        private TokenService Service(string secret = Secret) =>
            new(Options.Create(new AuthOptions { JwtSecret = secret, AccessTokenMinutes = 60 }), _accounts, _clock);

        [Fact]
        public async Task IssuedToken_ValidatesToItsAccount()
        {
            var account = Guid.NewGuid();
            var issued = await Service().IssueAsync(account, default);

            var result = await Service().ValidateAccessTokenAsync(issued.accessToken);
            Assert.True(result.IsValid);
            Assert.Equal(account, result.AccountId);
        }

        [Fact]
        public async Task ExpiredToken_IsRejectedAsExpired()
        {
            var issued = await Service().IssueAsync(Guid.NewGuid(), default);
            _clock.Advance(TimeSpan.FromMinutes(62));

            var result = await Service().ValidateAccessTokenAsync(issued.accessToken);
            Assert.False(result.IsValid);
            Assert.Equal(HandshakeRejectReasons.TokenExpired, result.Reason);
        }

        [Fact]
        public async Task TokenSignedWithAnotherKey_IsInvalid()
        {
            var forged = await Service("another-secret-another-secret-another-01").IssueAsync(Guid.NewGuid(), default);

            var result = await Service().ValidateAccessTokenAsync(forged.accessToken);
            Assert.False(result.IsValid);
            Assert.Equal(HandshakeRejectReasons.TokenInvalid, result.Reason);
        }

        [Fact]
        public async Task Refresh_RotatesAndExpires()
        {
            var service = Service();
            var account = Guid.NewGuid();
            var first = await service.IssueAsync(account, default);

            var second = await service.RefreshAsync(first.refreshToken, default);
            Assert.NotNull(second);
            Assert.Equal(account.ToString("N"), second!.accountId);
            Assert.NotEqual(first.refreshToken, second.refreshToken);
            Assert.Null(await service.RefreshAsync(first.refreshToken, default)); // already used

            _clock.Advance(TimeSpan.FromDays(31));
            Assert.Null(await service.RefreshAsync(second.refreshToken, default)); // expired
        }

        [Fact]
        public void ShortSecret_IsRefusedAtStartup()
        {
            Assert.Throws<InvalidOperationException>(() => Service("too-short"));
        }
    }
}
