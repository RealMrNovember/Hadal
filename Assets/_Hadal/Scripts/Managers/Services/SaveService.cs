using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Core.Services;
using Hadal.Data.Models;

namespace Hadal.Managers.Services
{
    public sealed class SaveService : ISaveService, IGameService
    {
        private const string SaveFolder = "save";
        private const string SaveFileName = "hadal_save.json";

        private readonly List<ISaveParticipant> _participants = new();
        private readonly string _savePath;

        public bool HasSave => File.Exists(_savePath);

        public SaveService()
        {
            var directory = Path.Combine(Application.persistentDataPath, SaveFolder);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            _savePath = Path.Combine(directory, SaveFileName);
        }

        public void Initialize() { }

        public void Shutdown() { }

        public void RegisterParticipant(ISaveParticipant participant)
        {
            if (participant == null)
                throw new ArgumentNullException(nameof(participant));

            if (!_participants.Contains(participant))
                _participants.Add(participant);
        }

        public void SaveAll()
        {
            var data = new SaveGameData
            {
                version = SaveGameData.CurrentVersion,
                savedAtUtcTicks = DateTime.UtcNow.Ticks
            };

            foreach (var participant in _participants)
                participant.CaptureSave(data);

            var json = JsonUtility.ToJson(data, prettyPrint: false);
            File.WriteAllText(_savePath, json);
        }

        public bool TryLoadAll()
        {
            if (!HasSave)
                return false;

            try
            {
                var json = File.ReadAllText(_savePath);
                var data = JsonUtility.FromJson<SaveGameData>(json);

                if (data.version > SaveGameData.CurrentVersion)
                {
                    Debug.LogError("[SaveService] Save version is newer than supported client.");
                    return false;
                }

                foreach (var participant in _participants)
                    participant.RestoreSave(data);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Failed to load save: {ex.Message}");
                return false;
            }
        }

        public void DeleteSave()
        {
            if (File.Exists(_savePath))
                File.Delete(_savePath);
        }
    }
}
