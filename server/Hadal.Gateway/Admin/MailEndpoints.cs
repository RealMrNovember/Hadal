using Hadal.Game;
using Hadal.Game.Mailbox;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;

namespace Hadal.Gateway.Admin
{
    /// <param name="Sender">Shown as the sender; empty = the game's own name for Overseer mail.</param>
    /// <param name="Amounts">Resources by type name, like a grant (only positive amounts).</param>
    /// <param name="Items">Grantable item ids with counts, like a grant.</param>
    public sealed record MailRequest(string Title, string Body, string? Sender = null, Dictionary<string, long>? Amounts = null,
        Dictionary<string, int>? Items = null, string? Note = null);

    /// <summary>Overseer mail (docs/29 §2.1): to one player or to everyone, with attachments the player claims in game.</summary>
    public static class MailEndpoints
    {
        public static void MapMailEndpoints(this RouteGroupBuilder api)
        {
            api.MapPost("/players/{id:guid}/mail", async (AdminPrincipal me, Guid id, MailRequest request,
                HttpContext http, GameWorld world, IAdminStore store, CancellationToken ct) =>
            {
                var (error, draft) = Draft(request, world);
                if (draft == null)
                    return Results.BadRequest(new { error });
                if (!await world.SendMailAsync(id.ToString("N"), draft, ct))
                    return Results.NotFound(new { error = "PlayerNotStarted" });

                await store.AuditAsync(me.Username, "player.mail", id.ToString("N"), AuditText(draft, request.Note), AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { ok = true });
            }).RequireAdmin(AdminRoles.GameMaster);

            api.MapPost("/mail/all", async (AdminPrincipal me, MailRequest request,
                HttpContext http, GameWorld world, IAdminStore store, CancellationToken ct) =>
            {
                var (error, draft) = Draft(request, world);
                if (draft == null)
                    return Results.BadRequest(new { error });
                var sent = await world.SendMailToAllAsync(draft, ct);

                await store.AuditAsync(me.Username, "mail.all", "*", $"[{sent}] {AuditText(draft, request.Note)}", AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { ok = true, sent });
            }).RequireAdmin(AdminRoles.GameMaster);
        }

        private static (string Error, MailDraft? Draft) Draft(MailRequest request, GameWorld world)
        {
            var title = (request.Title ?? string.Empty).Trim();
            if (title.Length == 0 || title.Length > MailRules.MaxTitle)
                return ("InvalidTitle", null);
            var body = (request.Body ?? string.Empty).Trim();
            if (body.Length > MailRules.MaxBody)
                return ("InvalidBody", null);

            var amounts = (AdminEndpoints.ParseAmounts(request.Amounts) ?? new Dictionary<ResourceType, long>())
                .Where(p => p.Value > 0).ToDictionary(p => p.Key, p => p.Value);
            var items = (request.Items ?? new Dictionary<string, int>()).Where(p => p.Value > 0 && world.IsGrantableItem(p.Key))
                .ToDictionary(p => p.Key, p => p.Value);
            return (string.Empty, new MailDraft(MailKinds.Admin, (request.Sender ?? string.Empty).Trim(), title, body, MailAttachments.From(amounts, items)));
        }

        private static string AuditText(MailDraft draft, string? note)
        {
            var attachments = string.Join(" ", draft.Attachments.Amounts.Select(p => $"{p.Key}:{p.Value}")
                .Concat(draft.Attachments.Items.Select(p => $"{p.Key}x{p.Value}")));
            return $"\"{draft.Title}\" {attachments} {note}".Trim();
        }
    }
}
