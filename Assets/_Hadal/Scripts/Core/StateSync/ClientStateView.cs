using Hadal.Data.Models;

namespace Hadal.Core.StateSync
{
    /// <summary>
    /// Non-authoritative in-memory visual state cache (RAM only — never written to disk).
    /// </summary>
    public sealed class ClientStateView
    {
        private SaveGameData _data = new();

        public SaveGameData Data => _data;

        public bool HasPersistedData { get; private set; }

        public void Replace(SaveGameData data)
        {
            _data = data ?? new SaveGameData();
            HasPersistedData = true;
        }

        public void ResetSession()
        {
            _data = new SaveGameData
            {
                version = SaveGameData.CurrentVersion
            };
            HasPersistedData = false;
        }
        public void MarkCaptured() => HasPersistedData = true;
    }
}
