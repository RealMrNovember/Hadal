using Microsoft.Extensions.Logging;

namespace Hadal.Game.Social
{
    /// <summary>Loads the alliance documents (logs and the alliance economy, docs/31) once at start-up.</summary>
    public sealed partial class SocialHub
    {
        private async Task LoadDocsAsync(CancellationToken cancellationToken)
        {
            var docs = await _store.LoadDocsAsync(cancellationToken);
            lock (_gate)
            {
                LoadLogs(docs);
                LoadPurses(docs);
            }
            _logger.LogInformation("Social hub: {Docs} alliance documents.", docs.Count);
        }
    }
}
