using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hadal.Gateway.Auth;
using HADAL.Shared.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hadal.Game.Tests
{
    /// <summary>Profile, transfer code and Google / Apple linking over the real HTTP pipeline.</summary>
    public sealed class AccountApiTests : IClassFixture<AccountApiTests.Factory>
    {
        private static readonly System.Text.Json.JsonSerializerOptions Json = GatewayEndToEndTests.Json;
        private readonly Factory _factory;

        public AccountApiTests(Factory factory)
        {
            _factory = factory;
        }

        /// <summary>"good:&lt;subject&gt;" is a valid Google token; Apple is left unconfigured.</summary>
        private sealed class FakeIdentities : IIdentityTokenValidator
        {
            public Task<IdentityResult> ValidateAsync(string provider, string idToken, CancellationToken cancellationToken) =>
                Task.FromResult(provider switch
                {
                    AccountProviders.Google when idToken.StartsWith("good:") => new IdentityResult(IdentityStatus.Valid, idToken[5..]),
                    AccountProviders.Google => new IdentityResult(IdentityStatus.Invalid, string.Empty),
                    AccountProviders.Apple => new IdentityResult(IdentityStatus.NotConfigured, string.Empty),
                    _ => new IdentityResult(IdentityStatus.UnknownProvider, string.Empty)
                });
        }

        public sealed class Factory : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseSetting("Game:CatalogPath", Path.Combine(AppContext.BaseDirectory, "catalog", "catalog.json"));
                builder.UseSetting("ConnectionStrings:Hadal", string.Empty);
                builder.UseSetting("Auth:JwtSecret", "test-secret-test-secret-test-secret-0123");
                builder.UseSetting("Auth:TransferLoginsPerHour", "1000");
                builder.UseSetting("Auth:GuestLoginsPerHour", "1000");
                builder.ConfigureTestServices(services => services.AddSingleton<IIdentityTokenValidator, FakeIdentities>());
            }
        }

        private async Task<AuthResponse> GuestAsync()
        {
            var response = await _factory.CreateClient().PostAsJsonAsync(AuthRoutes.Guest, new GuestLoginRequest(), Json);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
        }

        private HttpClient Client(string? accessToken = null)
        {
            var client = _factory.CreateClient();
            if (accessToken != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }

        private static async Task<string> ErrorOf(HttpResponseMessage response) =>
            (await response.Content.ReadFromJsonAsync<AccountError>(Json))!.error;

        [Fact]
        public async Task NewGuest_HasADefaultName_AndNoLinks()
        {
            var login = await GuestAsync();
            var profile = await Client(login.accessToken).GetFromJsonAsync<ProfileResponse>(AuthRoutes.Profile, Json);

            Assert.Equal(login.accountId, profile!.accountId);
            Assert.Matches("^Diver-[0-9]{4}$", profile.displayName);
            Assert.Empty(profile.linkedProviders);
        }

        [Fact]
        public async Task Profile_RequiresAValidAccessToken()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await Client().GetAsync(AuthRoutes.Profile)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Client("not-a-jwt").GetAsync(AuthRoutes.Profile)).StatusCode);
        }

        [Theory]
        [InlineData("Kaptan Deniz", true)]
        [InlineData("Øcean_Ünal-7", true)]
        [InlineData("ab", false)]
        [InlineData("<script>", false)]
        [InlineData("     ", false)]
        [InlineData("waytoolongname_1234", false)]
        [InlineData("Dr. Kaptan'ın", true)]
        [InlineData("★Deniz★", true)]
        public async Task DisplayName_IsValidated(string name, bool accepted)
        {
            var login = await GuestAsync();
            var response = await Client(login.accessToken).PutAsJsonAsync(AuthRoutes.Profile, new UpdateProfileRequest { displayName = name }, Json);

            if (accepted)
            {
                response.EnsureSuccessStatusCode();
                Assert.Equal(name, (await response.Content.ReadFromJsonAsync<ProfileResponse>(Json))!.displayName);
            }
            else
            {
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Equal(AccountErrors.NameInvalid, await ErrorOf(response));
            }
        }

        [Theory]
        [InlineData("Or0spu Kaptan")]
        [InlineData("S.i_k Deniz")]
        public async Task DisplayName_RefusesProfanity(string name)
        {
            var login = await GuestAsync();
            var response = await Client(login.accessToken).PutAsJsonAsync(AuthRoutes.Profile, new UpdateProfileRequest { displayName = name }, Json);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(AccountErrors.NameProfane, await ErrorOf(response));
        }

        [Fact]
        public async Task DisplayName_IsTrimmed()
        {
            var login = await GuestAsync();
            var response = await Client(login.accessToken).PutAsJsonAsync(AuthRoutes.Profile, new UpdateProfileRequest { displayName = "  Nemo  " }, Json);
            response.EnsureSuccessStatusCode();
            Assert.Equal("Nemo", (await response.Content.ReadFromJsonAsync<ProfileResponse>(Json))!.displayName);
        }

        [Fact]
        public async Task TransferCode_SignsInToTheSameAccount_OnAnotherDevice()
        {
            var original = await GuestAsync();
            var created = await Client(original.accessToken).PostAsJsonAsync(AuthRoutes.TransferCreate, new TransferCreateRequest { password = "deep-blue-42" }, Json);
            created.EnsureSuccessStatusCode();
            var code = (await created.Content.ReadFromJsonAsync<TransferCodeResponse>(Json))!.transferCode;
            Assert.Matches("^[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}$", code);

            // Typed on the new phone in lower case without dashes.
            var login = await Client().PostAsJsonAsync(AuthRoutes.TransferLogin,
                new TransferLoginRequest { transferCode = code.Replace("-", string.Empty).ToLowerInvariant(), password = "deep-blue-42" }, Json);
            login.EnsureSuccessStatusCode();
            var restored = (await login.Content.ReadFromJsonAsync<AuthResponse>(Json))!;

            Assert.Equal(original.accountId, restored.accountId);
            var profile = await Client(restored.accessToken).GetFromJsonAsync<ProfileResponse>(AuthRoutes.Profile, Json);
            Assert.Contains(AccountProviders.Transfer, profile!.linkedProviders);
        }

        [Fact]
        public async Task TransferLogin_WrongPasswordAndUnknownCode_LookTheSame()
        {
            var original = await GuestAsync();
            var created = await Client(original.accessToken).PostAsJsonAsync(AuthRoutes.TransferCreate, new TransferCreateRequest { password = "correct-horse" }, Json);
            var code = (await created.Content.ReadFromJsonAsync<TransferCodeResponse>(Json))!.transferCode;

            var wrongPassword = await Client().PostAsJsonAsync(AuthRoutes.TransferLogin, new TransferLoginRequest { transferCode = code, password = "wrong-horse" }, Json);
            var unknownCode = await Client().PostAsJsonAsync(AuthRoutes.TransferLogin, new TransferLoginRequest { transferCode = "AAAA-BBBB-CCCC", password = "correct-horse" }, Json);

            Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, unknownCode.StatusCode);
            Assert.Equal(await ErrorOf(wrongPassword), await ErrorOf(unknownCode));
        }

        [Fact]
        public async Task NewTransferCode_RetiresTheOldOne()
        {
            var original = await GuestAsync();
            var client = Client(original.accessToken);
            var first = (await (await client.PostAsJsonAsync(AuthRoutes.TransferCreate, new TransferCreateRequest { password = "password-one" }, Json))
                .Content.ReadFromJsonAsync<TransferCodeResponse>(Json))!.transferCode;
            await client.PostAsJsonAsync(AuthRoutes.TransferCreate, new TransferCreateRequest { password = "password-two" }, Json);

            var old = await Client().PostAsJsonAsync(AuthRoutes.TransferLogin, new TransferLoginRequest { transferCode = first, password = "password-one" }, Json);
            Assert.Equal(HttpStatusCode.Unauthorized, old.StatusCode);
        }

        [Fact]
        public async Task ShortPassword_IsRejected()
        {
            var login = await GuestAsync();
            var response = await Client(login.accessToken).PostAsJsonAsync(AuthRoutes.TransferCreate, new TransferCreateRequest { password = "short" }, Json);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(AccountErrors.PasswordInvalid, await ErrorOf(response));
        }

        [Fact]
        public async Task GoogleLink_ThenSignIn_ReturnsTheLinkedAccount()
        {
            var original = await GuestAsync();
            var subject = "google-user-" + Guid.NewGuid().ToString("N");
            var link = await Client(original.accessToken).PostAsJsonAsync(AuthRoutes.ProviderLink,
                new ProviderRequest { provider = AccountProviders.Google, idToken = "good:" + subject }, Json);
            link.EnsureSuccessStatusCode();
            Assert.Contains(AccountProviders.Google, (await link.Content.ReadFromJsonAsync<ProfileResponse>(Json))!.linkedProviders);

            var signIn = await Client().PostAsJsonAsync(AuthRoutes.ProviderLogin,
                new ProviderRequest { provider = AccountProviders.Google, idToken = "good:" + subject }, Json);
            signIn.EnsureSuccessStatusCode();
            Assert.Equal(original.accountId, (await signIn.Content.ReadFromJsonAsync<AuthResponse>(Json))!.accountId);
        }

        [Fact]
        public async Task GoogleIdentity_CannotBeLinkedToTwoAccounts()
        {
            var subject = "google-user-" + Guid.NewGuid().ToString("N");
            var first = await GuestAsync();
            var second = await GuestAsync();
            var request = new ProviderRequest { provider = AccountProviders.Google, idToken = "good:" + subject };

            (await Client(first.accessToken).PostAsJsonAsync(AuthRoutes.ProviderLink, request, Json)).EnsureSuccessStatusCode();
            var conflict = await Client(second.accessToken).PostAsJsonAsync(AuthRoutes.ProviderLink, request, Json);

            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            Assert.Equal(AccountErrors.ProviderLinkedElsewhere, await ErrorOf(conflict));
        }

        [Fact]
        public async Task GoogleLink_SameIdentityAgain_IsIdempotent()
        {
            var account = await GuestAsync();
            var request = new ProviderRequest { provider = AccountProviders.Google, idToken = "good:google-user-" + Guid.NewGuid().ToString("N") };

            (await Client(account.accessToken).PostAsJsonAsync(AuthRoutes.ProviderLink, request, Json)).EnsureSuccessStatusCode();
            var again = await Client(account.accessToken).PostAsJsonAsync(AuthRoutes.ProviderLink, request, Json);

            again.EnsureSuccessStatusCode();
            Assert.Contains(AccountProviders.Google, (await again.Content.ReadFromJsonAsync<ProfileResponse>(Json))!.linkedProviders);
        }

        [Fact]
        public void GoogleClientIds_AreReadFromAuthConfiguration()
        {
            // docker-compose passes HADAL_GOOGLE_CLIENT_IDS as Auth__GoogleClientIds, i.e. Auth:GoogleClientIds.
            const string web = "1052662691515-web.apps.googleusercontent.com";
            using var configured = _factory.WithWebHostBuilder(b => b.UseSetting("Auth:GoogleClientIds", web));
            Assert.Equal(web, configured.Services.GetRequiredService<IOptions<AuthOptions>>().Value.GoogleClientIds);
        }

        [Fact]
        public async Task GoogleValidator_IsEnabledOnlyByConfiguredClientIds()
        {
            var off = new OidcIdentityTokenValidator(Options.Create(new AuthOptions()));
            Assert.Equal(IdentityStatus.NotConfigured, (await off.ValidateAsync(AccountProviders.Google, "token", default)).Status);

            // Configured: the provider is live, so a token is judged on its own (empty / oversized fail before any key fetch).
            var on = new OidcIdentityTokenValidator(Options.Create(new AuthOptions { GoogleClientIds = " web.apps.googleusercontent.com , android.apps.googleusercontent.com " }));
            Assert.Equal(IdentityStatus.Invalid, (await on.ValidateAsync(AccountProviders.Google, string.Empty, default)).Status);
            Assert.Equal(IdentityStatus.Invalid, (await on.ValidateAsync(AccountProviders.Google, new string('x', 9000), default)).Status);
            Assert.Equal(IdentityStatus.NotConfigured, (await on.ValidateAsync(AccountProviders.Apple, "token", default)).Status);
            Assert.Equal(IdentityStatus.UnknownProvider, (await on.ValidateAsync("facebook", "token", default)).Status);
        }

        [Theory]
        [InlineData(AccountProviders.Google, "forged", HttpStatusCode.Unauthorized, AccountErrors.ProviderTokenInvalid)]
        [InlineData(AccountProviders.Apple, "anything", HttpStatusCode.NotImplemented, AccountErrors.ProviderNotConfigured)]
        [InlineData("facebook", "anything", HttpStatusCode.BadRequest, AccountErrors.ProviderUnknown)]
        [InlineData(AccountProviders.Google, "good:never-linked", HttpStatusCode.NotFound, AccountErrors.ProviderNotLinked)]
        public async Task ProviderSignIn_Failures(string provider, string token, HttpStatusCode status, string error)
        {
            var response = await Client().PostAsJsonAsync(AuthRoutes.ProviderLogin, new ProviderRequest { provider = provider, idToken = token }, Json);
            Assert.Equal(status, response.StatusCode);
            Assert.Equal(error, await ErrorOf(response));
        }

        [Fact]
        public void PasswordHash_VerifiesOnlyTheRightPassword()
        {
            var hash = PasswordHasher.Hash("abyssal-pressure");
            Assert.StartsWith("pbkdf2$", hash);
            Assert.True(PasswordHasher.Verify("abyssal-pressure", hash));
            Assert.False(PasswordHasher.Verify("abyssal-pressurE", hash));
            Assert.False(PasswordHasher.Verify("abyssal-pressure", "garbage"));
        }
    }
}
