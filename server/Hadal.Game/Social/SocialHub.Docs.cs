using Microsoft.Extensions.Logging;

namespace Hadal.Game.Social
{
    /// <summary>
    /// Loads the alliance documents (logs and the alliance economy, docs/31) once at start-up. Documents of alliances
    /// that no longer exist ("a:&lt;allianceId&gt;:…" keys left by a disband) are not loaded and are deleted from the store;
    /// a disband removes its alliance's documents at once (<see cref="ForgetAllianceDocs"/>).
    /// </summary>
    public sealed partial class SocialHub
    {
        private async Task LoadDocsAsync(CancellationToken cancellationToken)
        {
            var docs = await _store.LoadDocsAsync(cancellationToken);
            List<string> orphans;
            lock (_gate)
            {
                orphans = docs.Select(d => AllianceOfDoc(d.Key)).OfType<string>().Where(id => !_alliances.ContainsKey(id))
                    .Distinct(StringComparer.Ordinal).ToList();
                var kept = docs.Where(d => AllianceOfDoc(d.Key) is not { } id || _alliances.ContainsKey(id)).ToList();
                LoadLogs(kept);
                LoadPurses(kept);
                LoadGifts(kept);
                LoadResearch(kept);
                LoadKills(kept);
            }

            foreach (var allianceId in orphans)
                await _store.DeleteDocsAsync(DocPrefix(allianceId), cancellationToken);
            if (orphans.Count > 0)
                _logger.LogInformation("Social hub: removed the documents of {Count} disbanded alliances.", orphans.Count);
            _logger.LogInformation("Social hub: {Docs} alliance documents.", docs.Count);
        }

        /// <summary>The alliance an "a:&lt;allianceId&gt;:&lt;kind&gt;" document belongs to; null for other keys (player purses).</summary>
        internal static string? AllianceOfDoc(string key)
        {
            if (!key.StartsWith("a:", StringComparison.Ordinal))
                return null;
            var end = key.IndexOf(':', 2);
            return end > 2 ? key.Substring(2, end - 2) : null;
        }

        private static string DocPrefix(string allianceId) => "a:" + allianceId + ":";
        /// <summary>A disbanded alliance's logs, gifts, research and kill tally go, in memory and in the store. Caller holds the lock.</summary>
        private void ForgetAllianceDocs(string allianceId)
        {
            var prefix = DocPrefix(allianceId);
            foreach (var key in _logs.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
                _logs.Remove(key);
            _gifts.Remove(allianceId);
            _research.Remove(allianceId);
            _kills.Remove(allianceId);
            Write(ct => _store.DeleteDocsAsync(prefix, ct));
        }
    }
}
