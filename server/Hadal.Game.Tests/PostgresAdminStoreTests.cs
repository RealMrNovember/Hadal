using Hadal.Gateway.Admin;
using Hadal.Gateway.Auth;
using HADAL.Shared.Enums;
using Npgsql;

namespace Hadal.Game.Tests
{
    /// <summary>Overseer SQL against a real PostgreSQL (HADAL_TEST_PG); skipped when unset.</summary>
    public sealed class PostgresAdminStoreTests
    {
        private static readonly string? ConnectionString = PostgresTest.ConnectionString;

        [Fact]
        public async Task Admins_Coupons_Bans_LiveOps_Catalogs_Search()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return; // not configured on this machine

            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            var accounts = new PostgresAccountStore(dataSource);
            var store = new PostgresAdminStore(dataSource);
            await accounts.InitializeAsync(default);
            await store.InitializeAsync(default);
            await store.InitializeAsync(default); // idempotent

            var suffix = Guid.NewGuid().ToString("N")[..8];
            var a = await accounts.CreateGuestAsync("pgtest", "0.1.0", default);
            var b = await accounts.CreateGuestAsync("pgtest", "0.1.0", default);
            await accounts.SetDisplayNameAsync(a, "Nemo " + suffix, default);
            var code = "PG" + suffix.ToUpperInvariant();

            try
            {
                // Admins
                var (admin, _) = AdminAuth.NewAdmin("pg-" + suffix, AdminRoles.GameMaster);
                Assert.True(await store.CreateAdminAsync(admin, default));
                Assert.False(await store.CreateAdminAsync(admin, default));
                Assert.Equal(admin.Id, (await store.FindAdminAsync(admin.Username.ToUpperInvariant(), default))!.Id);
                await store.AuditAsync(admin.Username, "test", "t", "d", "127.0.0.1", default);
                Assert.Contains(await store.ListAuditAsync(10, default), e => e.Admin == admin.Username && e.Action == "test");

                // Coupons: one per player, limit respected, undo restores
                var rewards = new Dictionary<ResourceType, long> { [ResourceType.Titanium] = 300 };
                Assert.True(await store.CreateCouponAsync(new Coupon(code, rewards, 1, 0, null, true, admin.Username, DateTimeOffset.UtcNow, "pg"), default));
                var now = DateTimeOffset.UtcNow;
                Assert.Equal(RedeemResult.Redeemed, (await store.RedeemAsync(code, a, now, default)).Result);
                Assert.Equal(RedeemResult.AlreadyRedeemed, (await store.RedeemAsync(code, a, now, default)).Result);
                Assert.Equal(RedeemResult.Exhausted, (await store.RedeemAsync(code, b, now, default)).Result);
                await store.UndoRedemptionAsync(code, a, default);
                Assert.Equal(RedeemResult.Redeemed, (await store.RedeemAsync(code, b, now, default)).Result);
                Assert.Equal(300, (await store.ListCouponsAsync(default)).Single(c => c.Code == code).Rewards[ResourceType.Titanium]);

                // Bans
                await store.SetBanAsync(a, now.AddHours(1), "cheating", default);
                Assert.Equal("cheating", (await store.GetActiveBanAsync(a, now, default))!.Value.Reason);
                Assert.Null(await store.GetActiveBanAsync(a, now.AddHours(2), default));
                await store.SetBanAsync(a, null, string.Empty, default);
                Assert.Null(await store.GetActiveBanAsync(a, now, default));

                // Live ops
                await store.SetLiveOpsAsync(new LiveOpsSettings { Maintenance = true, Announcement = "Merhaba" }, default);
                var liveOps = await store.GetLiveOpsAsync(default);
                Assert.True(liveOps.Maintenance);
                Assert.Equal("Merhaba", liveOps.Announcement);
                await store.SetLiveOpsAsync(new LiveOpsSettings(), default);

                // Catalog versions
                var version = await store.AddCatalogAsync("{\"v\":1}", admin.Username, "pg", default);
                Assert.Equal("{\"v\":1}", (await store.GetCatalogAsync(version, default))!.Json);
                Assert.Equal(version, (await store.GetLatestCatalogAsync(default))!.Version);
                Assert.Contains(await store.ListCatalogsAsync(5, default), c => c.Version == version && c.Json == string.Empty);

                // Search by name and by account id prefix
                Assert.Contains(await store.SearchPlayersAsync(suffix, 10, default), p => p.AccountId == a);
                Assert.Contains(await store.SearchPlayersAsync(b.ToString("N")[..10], 10, default), p => p.AccountId == b);
                var (total, last24h) = await store.CountPlayersAsync(now, default);
                Assert.True(total >= 2 && last24h >= 2);
            }
            finally
            {
                await using var cleanup = dataSource.CreateCommand(
                    @"DELETE FROM hadal.coupons WHERE code = @c; DELETE FROM hadal.admins WHERE username LIKE 'pg-%';
                      DELETE FROM hadal.catalog_versions WHERE note = 'pg'; DELETE FROM hadal.accounts WHERE account_id = ANY(@ids)");
                cleanup.Parameters.AddWithValue("c", code);
                cleanup.Parameters.AddWithValue("ids", new[] { a, b });
                await cleanup.ExecuteNonQueryAsync();
            }
        }
    }
}
