using Hadal.Game;
using Hadal.Game.Kingdoms;

namespace Hadal.Gateway.Admin
{
    /// <param name="Status">"open", "full" or "closed"; null = unchanged.</param>
    /// <param name="PlayerCap">New players stop at this many members; null = unchanged.</param>
    /// <param name="OpenDays">New players stop this many days after opening (0 = never); null = unchanged.</param>
    public sealed record KingdomUpdateRequest(string? Status = null, int? PlayerCap = null, int? OpenDays = null, string? Name = null);

    /// <summary>Kingdoms on the Overseer panel (docs/39 §5): the list with counts, open the next, status, cap, window.</summary>
    public static class KingdomEndpoints
    {
        public static void MapKingdomEndpoints(this RouteGroupBuilder api)
        {
            api.MapGet("/kingdoms", (AdminPrincipal _, GameWorld world, IClock clock) =>
                Results.Ok(world.Kingdoms.List(clock.UtcNow).Select(k => Json(k.Kingdom, k.Members, k.Active)))).RequireAdmin(AdminRoles.Viewer);

            api.MapPost("/kingdoms/open", async (AdminPrincipal me, HttpContext http, GameWorld world, IAdminStore store, CancellationToken ct) =>
            {
                var opened = await world.OpenKingdomAsync(ct);
                await store.AuditAsync(me.Username, "kingdom.open", opened.Id, $"#{opened.Number}", AdminAuth.ClientIp(http), ct);
                return Results.Ok(Json(opened, 0, 0));
            }).RequireAdmin(AdminRoles.Owner);

            api.MapPost("/kingdoms/{id}", async (AdminPrincipal me, string id, KingdomUpdateRequest request, HttpContext http, GameWorld world,
                IAdminStore store, CancellationToken ct) =>
            {
                var changed = await world.Kingdoms.UpdateAsync(id, request.Status, request.PlayerCap, request.OpenDays, request.Name, ct);
                if (changed == null)
                    return Results.BadRequest(new { error = "InvalidKingdom" });
                await store.AuditAsync(me.Username, "kingdom.update", id,
                    $"status={changed.Status} cap={changed.PlayerCap} days={changed.OpenDays} name={changed.Name}", AdminAuth.ClientIp(http), ct);
                return Results.Ok(Json(changed, 0, 0));
            }).RequireAdmin(AdminRoles.Owner);
        }

        private static object Json(Kingdom k, int members, int active) => new
        {
            k.Id, k.Number, k.Name, k.OpenedAt, k.Status, k.PlayerCap, k.OpenDays, members, active
        };
    }
}
