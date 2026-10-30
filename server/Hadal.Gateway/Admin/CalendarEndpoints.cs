using Hadal.Game;
using Hadal.Game.Events;
using HADAL.Shared.Simulation;

namespace Hadal.Gateway.Admin
{
    public sealed record ScheduleEnableRequest(bool Enabled);

    /// <summary>
    /// Overseer API for the weekly calendar (docs/29 §6, roadmap §7): recurring schedules, the week grid, switching a
    /// schedule on or off, editing its days and times, making one from an existing event, and creating the next
    /// occurrence at once. Every change is audited.
    /// </summary>
    public static class CalendarEndpoints
    {
        public static void MapCalendarEndpoints(this RouteGroupBuilder api)
        {
            api.MapGet("/schedules", (AdminPrincipal _, GameWorld world, IClock clock) =>
            {
                var now = clock.UtcNow;
                return Results.Ok(world.Events.Schedules().Select(s => ScheduleJson(s, now)));
            }).RequireAdmin(AdminRoles.Viewer);

            api.MapGet("/schedules/{id}", (AdminPrincipal _, string id, GameWorld world, IClock clock) =>
            {
                var schedule = world.Events.Schedule(id);
                return schedule == null ? Results.NotFound() : Results.Ok(ScheduleJson(schedule, clock.UtcNow));
            }).RequireAdmin(AdminRoles.Viewer);

            // The week grid: today (UTC) and the following days, every occurrence per kingdom.
            api.MapGet("/calendar", (AdminPrincipal _, int? days, GameWorld world, IClock clock) =>
            {
                var now = clock.UtcNow;
                var count = Math.Clamp(days ?? EventCalendar.Days, 1, 28);
                var from = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
                var to = from.AddDays(count);
                var entries = world.Events.Calendar(from, to, now);
                return Results.Ok(new
                {
                    now,
                    from,
                    days = Enumerable.Range(0, count).Select(i => from.AddDays(i)).ToArray(),
                    kingdoms = world.Kingdoms.List(now).Select(k => new { id = k.Kingdom.Id, number = k.Kingdom.Number, name = k.Kingdom.Name }),
                    entries = entries.Select(e => new
                    {
                        id = e.Id, scheduleId = e.ScheduleId, template = e.Template, title = e.Title, banner = e.Banner, startsAt = e.StartsAt, endsAt = e.EndsAt,
                        kingdom = e.Kingdom, minLevel = e.MinLevel, maxLevel = e.MaxLevel, state = e.State
                    })
                });
            }).RequireAdmin(AdminRoles.Viewer);

            api.MapPost("/schedules", async (AdminPrincipal me, EventSchedule request, HttpContext http, GameWorld world, IClock clock, IAdminStore store,
                CancellationToken ct) =>
            {
                var existed = !string.IsNullOrEmpty(request.Id) && world.Events.Schedule(request.Id) != null;
                var (error, saved) = world.Events.SaveSchedule(request, me.Username, clock.UtcNow);
                if (error.Length > 0)
                    return Results.BadRequest(new { error });
                await store.AuditAsync(me.Username, existed ? "schedule.update" : "schedule.create", saved!.Id, Describe(saved), AdminAuth.ClientIp(http), ct);
                return Results.Ok(ScheduleJson(saved, clock.UtcNow));
            }).RequireAdmin(AdminRoles.GameMaster);

            api.MapPost("/schedules/{id}/enabled", async (AdminPrincipal me, string id, ScheduleEnableRequest request, HttpContext http, GameWorld world, IClock clock,
                IAdminStore store, CancellationToken ct) =>
            {
                var error = world.Events.SetScheduleEnabled(id, request.Enabled, clock.UtcNow);
                if (error.Length > 0)
                    return Results.BadRequest(new { error });
                await store.AuditAsync(me.Username, request.Enabled ? "schedule.enable" : "schedule.disable", id, string.Empty, AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { ok = true });
            }).RequireAdmin(AdminRoles.GameMaster);

            api.MapPost("/schedules/from-event/{eventId}", async (AdminPrincipal me, string eventId, HttpContext http, GameWorld world, IClock clock, IAdminStore store,
                CancellationToken ct) =>
            {
                var (error, saved) = world.Events.ScheduleFromEvent(eventId, me.Username, clock.UtcNow);
                if (error.Length > 0)
                    return Results.BadRequest(new { error });
                await store.AuditAsync(me.Username, "schedule.from-event", saved!.Id, eventId, AdminAuth.ClientIp(http), ct);
                return Results.Ok(ScheduleJson(saved, clock.UtcNow));
            }).RequireAdmin(AdminRoles.GameMaster);

            api.MapPost("/schedules/{id}/run", async (AdminPrincipal me, string id, HttpContext http, GameWorld world, IClock clock, IAdminStore store, CancellationToken ct) =>
            {
                var (error, created) = world.Events.CreateNextOccurrence(id, clock.UtcNow);
                if (error.Length > 0)
                    return Results.BadRequest(new { error });
                await store.AuditAsync(me.Username, "schedule.run", id, string.Join(", ", created), AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { created });
            }).RequireAdmin(AdminRoles.GameMaster);

            api.MapDelete("/schedules/{id}", async (AdminPrincipal me, string id, HttpContext http, GameWorld world, IAdminStore store, CancellationToken ct) =>
            {
                var error = world.Events.DeleteSchedule(id);
                if (error.Length > 0)
                    return Results.BadRequest(new { error });
                await store.AuditAsync(me.Username, "schedule.delete", id, string.Empty, AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { ok = true });
            }).RequireAdmin(AdminRoles.GameMaster);
        }

        private static string Describe(EventSchedule s) =>
            $"{(s.Enabled ? "on" : "off")} days={string.Join(",", s.Weekdays)} {s.StartMinute / 60:00}:{s.StartMinute % 60:00} UTC {s.DurationHours}h";

        private static object ScheduleJson(EventSchedule s, DateTimeOffset now)
        {
            var next = EventScheduleRules.Starts(s, now, now.AddDays(8)).Cast<DateTimeOffset?>().FirstOrDefault();
            return new
            {
                schedule = s,
                nextStart = next
            };
        }
    }
}
