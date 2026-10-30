using Hadal.Game;
using Hadal.Game.Events;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;

namespace Hadal.Gateway.Admin
{
    public sealed record EventExtendRequest(double Hours);

    /// <summary>Overseer API for events (docs/29 §6): list, edit, publish, end, extend, copy, cancel, delete, rankings.</summary>
    public static class EventEndpoints
    {
        /// <summary>Banner art the client ships (Resources/Events/banner_{id}).</summary>
        public static readonly string[] Banners = { "abyss", "vent", "bloom", "leviathan", "trench", "wreck" };

        public static void MapEventEndpoints(this RouteGroupBuilder api)
        {
            api.MapGet("/events/meta", (AdminPrincipal _, GameWorld world) =>
            {
                var catalog = world.Catalog.Catalog;
                return Results.Ok(new
                {
                    templates = EventTemplates.All,
                    actions = EventActions.All,
                    banners = Banners,
                    resources = Enum.GetNames<ResourceType>(),
                    items = (catalog.Data.items ?? Array.Empty<HADAL.Shared.Catalog.ItemData>()).Select(i => new { id = i.id, kind = i.kind, rarity = i.rarity }),
                    heroes = catalog.Heroes.Select(h => new { id = h.Id, rarity = h.Rarity.id, basePower = h.Rarity.basePower }),
                    heroRules = new { maxStars = catalog.HeroRules.maxStars, levelCapPerStar = catalog.HeroRules.levelCapPerStar },
                    troops = new { types = catalog.Troops.types.Select(t => t.id), tiers = catalog.Troops.tiers.Length },
                    rarities = catalog.Heroes.Select(h => h.Rarity.id).Distinct(),
                    mobilizationKinds = MobilizationRules.Kinds.Select(k => new { id = k.Id, baseTarget = k.BaseTarget, actions = k.Actions }),
                    duelThemes = AllianceDuelRules.Themes,
                    duelDays = AllianceDuelRules.Days
                });
            }).RequireAdmin(AdminRoles.Viewer);

            // Live state of the alliance templates (docs/29 §6): Mobilization tables per alliance, duel pairings per kingdom.
            api.MapGet("/events/{id}/live", (AdminPrincipal _, string id, GameWorld world, IClock clock) =>
            {
                var summary = world.Events.Get(id, clock.UtcNow);
                if (summary == null)
                    return Results.NotFound();
                var now = clock.UtcNow;
                return Results.Ok(new
                {
                    template = summary.Definition.Template,
                    paidOutAt = summary.Definition.PaidOutAt,
                    paidOutMails = summary.Definition.PaidOutMails,
                    pairedAt = summary.Definition.DuelState.PairedAt,
                    mobilization = world.Events.MobilizationPools(id, now),
                    duel = world.Events.DuelPairings(id, now)
                });
            }).RequireAdmin(AdminRoles.Viewer);

            MapAction(api, "pair", (world, id, now, _) => world.Events.RepairDuel(id, now));

            api.MapGet("/events", (AdminPrincipal _, GameWorld world, IClock clock) =>
                Results.Ok(world.Events.List(clock.UtcNow).Select(EventJson))).RequireAdmin(AdminRoles.Viewer);

            api.MapGet("/events/{id}", (AdminPrincipal _, string id, GameWorld world, IClock clock) =>
            {
                var summary = world.Events.Get(id, clock.UtcNow);
                if (summary == null)
                    return Results.NotFound();
                var ranking = world.Events.Ranking(id, 100);
                return Results.Ok(new { @event = EventJson(summary), ranking = ranking?.Entries ?? Array.Empty<HADAL.Shared.DTOs.EventRankEntryDto>(), alliance = ranking?.Alliance ?? false });
            }).RequireAdmin(AdminRoles.Viewer);

            api.MapPost("/events", async (AdminPrincipal me, EventDefinition request, HttpContext http, GameWorld world, IClock clock, IAdminStore store, CancellationToken ct) =>
            {
                var (error, saved) = world.Events.Save(request, me.Username, clock.UtcNow);
                if (error.Length > 0)
                    return Results.BadRequest(new { error });
                await store.AuditAsync(me.Username, string.IsNullOrEmpty(request.Id) ? "event.create" : "event.update", saved!.Id,
                    saved.Title.TryGetValue("tr", out var title) ? title : saved.Title.Values.FirstOrDefault() ?? string.Empty, AdminAuth.ClientIp(http), ct);
                return Results.Ok(saved);
            }).RequireAdmin(AdminRoles.GameMaster);

            MapAction(api, "publish", (world, id, now, _) => world.Events.Publish(id, now));
            MapAction(api, "cancel", (world, id, now, _) => world.Events.Cancel(id, now));
            MapAction(api, "end", (world, id, now, _) => world.Events.EndNow(id, now));

            api.MapPost("/events/{id}/extend", async (AdminPrincipal me, string id, EventExtendRequest request, HttpContext http, GameWorld world, IClock clock, IAdminStore store,
                CancellationToken ct) =>
            {
                var error = world.Events.Extend(id, request.Hours, clock.UtcNow);
                if (error.Length > 0)
                    return Results.BadRequest(new { error });
                await store.AuditAsync(me.Username, "event.extend", id, $"{request.Hours}h", AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { ok = true });
            }).RequireAdmin(AdminRoles.GameMaster);

            api.MapPost("/events/{id}/duplicate", async (AdminPrincipal me, string id, HttpContext http, GameWorld world, IClock clock, IAdminStore store, CancellationToken ct) =>
            {
                var (error, copy) = world.Events.Duplicate(id, me.Username, clock.UtcNow);
                if (error.Length > 0)
                    return Results.BadRequest(new { error });
                await store.AuditAsync(me.Username, "event.duplicate", id, copy!.Id, AdminAuth.ClientIp(http), ct);
                return Results.Ok(copy);
            }).RequireAdmin(AdminRoles.GameMaster);

            api.MapDelete("/events/{id}", async (AdminPrincipal me, string id, HttpContext http, GameWorld world, IAdminStore store, CancellationToken ct) =>
            {
                var error = world.Events.Delete(id);
                if (error.Length > 0)
                    return Results.BadRequest(new { error });
                await store.AuditAsync(me.Username, "event.delete", id, string.Empty, AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { ok = true });
            }).RequireAdmin(AdminRoles.GameMaster);
        }

        private static void MapAction(RouteGroupBuilder api, string action, Func<GameWorld, string, DateTimeOffset, AdminPrincipal, string> apply) =>
            api.MapPost("/events/{id}/" + action, async (AdminPrincipal me, string id, HttpContext http, GameWorld world, IClock clock, IAdminStore store, CancellationToken ct) =>
            {
                var error = apply(world, id, clock.UtcNow, me);
                if (error.Length > 0)
                    return Results.BadRequest(new { error });
                await store.AuditAsync(me.Username, "event." + action, id, string.Empty, AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { ok = true });
            }).RequireAdmin(AdminRoles.GameMaster);

        private static object EventJson(EventService.EventSummary s) => new
        {
            definition = s.Definition,
            phase = s.Phase,
            participants = s.Participants,
            topPoints = s.TopPoints
        };
    }
}
