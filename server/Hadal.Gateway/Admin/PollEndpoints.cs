using Hadal.Game;

namespace Hadal.Gateway.Admin
{
    /// <summary>
    /// Alliance polls and time-slot votes on the Overseer panel (docs/29 §5): every alliance's votes read-only, with
    /// "close now" and "delete" for abusive ones. Both are audited.
    /// </summary>
    public static class PollEndpoints
    {
        public static void MapPollEndpoints(this RouteGroupBuilder api)
        {
            api.MapGet("/polls", (AdminPrincipal _, string? q, GameWorld world) =>
                Results.Ok(world.Social.PollsForOverseer(q).Select(p => new
                {
                    p.Id, p.AllianceId, p.AllianceTag, p.AllianceName, p.Kind, p.Question, p.Purpose, p.CreatorName, p.CreatedAt, p.ClosesAt,
                    p.ClosedAt, p.ClosedBy, p.Multiple, p.Anonymous, p.Voters, p.Winner,
                    Options = p.Options.Select(o => new { o.Text, o.AtMs, o.Votes })
                }))).RequireAdmin(AdminRoles.Viewer);

            api.MapPost("/polls/{id}/close", async (AdminPrincipal me, string id, HttpContext http, GameWorld world, IClock clock, IAdminStore store,
                CancellationToken ct) =>
            {
                var closed = world.Social.CloseByOverseer(id, clock.UtcNow);
                await store.AuditAsync(me.Username, "poll.close", id, closed ? string.Empty : "not found or closed", AdminAuth.ClientIp(http), ct);
                return closed ? Results.Ok(new { ok = true }) : Results.NotFound();
            }).RequireAdmin(AdminRoles.GameMaster);

            api.MapDelete("/polls/{id}", async (AdminPrincipal me, string id, HttpContext http, GameWorld world, IClock clock, IAdminStore store,
                CancellationToken ct) =>
            {
                var summary = world.Social.PollsForOverseer(null).FirstOrDefault(p => p.Id == id);
                var deleted = world.Social.DeletePoll(id, clock.UtcNow);
                await store.AuditAsync(me.Username, "poll.delete", id,
                    deleted ? $"[{summary?.AllianceTag}] {summary?.Question}".Trim() : "not found", AdminAuth.ClientIp(http), ct);
                return deleted ? Results.Ok(new { ok = true }) : Results.NotFound();
            }).RequireAdmin(AdminRoles.GameMaster);
        }
    }
}
