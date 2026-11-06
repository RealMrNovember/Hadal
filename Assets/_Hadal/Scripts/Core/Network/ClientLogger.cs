using System;
using System.Collections.Generic;
using Hadal.Data.Config;
using HADAL.Shared.Telemetry;
using UnityEngine;
using VContainer.Unity;

namespace Hadal.Core.Network
{
    /// <summary>
    /// Sends client crash/error reports to the server in small, throttled batches.
    /// Best-effort: a failed upload never surfaces to the player or blocks gameplay.
    /// </summary>
    public sealed class ClientLogger : IStartable, ITickable, IDisposable
    {
        private const int FlushIntervalSeconds = 60;
        private const int FlushCount = 10;
        private const int MaxBatch = 20;
        private const int MaxQueue = 100;

        private readonly IAuthService _auth;
        private readonly GameConfigSO _config;
        private readonly Queue<ClientLogEntry> _queue = new();
        private readonly object _gate = new();
        private double _nextFlush;
        private bool _hooked;

        public ClientLogger(IAuthService auth, GameConfigSO config)
        {
            _auth = auth;
            _config = config;
        }

        public void Start()
        {
            if (_hooked)
                return;
            Application.logMessageReceived += OnLogMessage;
            Application.quitting += OnQuitting;
            _hooked = true;
            _nextFlush = Time.time + FlushIntervalSeconds;
        }

        public void Tick()
        {
            if (Time.time >= _nextFlush || QueueCount() >= FlushCount)
                Flush();
        }

        public void Dispose()
        {
            if (!_hooked)
                return;
            Application.logMessageReceived -= OnLogMessage;
            Application.quitting -= OnQuitting;
            _hooked = false;
        }

        private void OnQuitting() => Flush();

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception)
                return;

            lock (_gate)
            {
                if (_queue.Count >= MaxQueue)
                    _queue.Dequeue();

                _queue.Enqueue(new ClientLogEntry
                {
                    level = type == LogType.Exception ? ClientLogLevels.Exception : ClientLogLevels.Error,
                    message = condition ?? string.Empty,
                    stack = stackTrace ?? string.Empty,
                    at = (long)(DateTimeOffset.UtcNow - UnixEpoch).TotalSeconds
                });
            }
        }

        private async void Flush()
        {
            _nextFlush = Time.time + FlushIntervalSeconds;

            List<ClientLogEntry> batch;
            lock (_gate)
            {
                batch = new List<ClientLogEntry>();
                while (_queue.Count > 0 && batch.Count < MaxBatch)
                    batch.Add(_queue.Dequeue());
            }

            if (batch.Count == 0)
                return;

            try
            {
                var token = await _auth.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    Requeue(batch);
                    return;
                }

                var body = new ClientLogBatch
                {
                    entries = batch.ToArray(),
                    clientVersion = Application.version,
                    platform = Application.platform.ToString(),
                    device = SystemInfo.deviceModel
                };
                await HttpJson.SendAsync<ClientLogAck>("POST", _config.ApiBaseUrl + TelemetryRoutes.ClientLog, body, token);
            }
            catch (Exception)
            {
                // Swallowed on purpose: telemetry must not crash the client. The batch goes back to the queue.
                Requeue(batch);
            }
        }

        private void Requeue(List<ClientLogEntry> batch)
        {
            lock (_gate)
            {
                foreach (var entry in batch)
                {
                    if (_queue.Count >= MaxQueue)
                        _queue.Dequeue();
                    _queue.Enqueue(entry);
                }
            }
        }

        private int QueueCount()
        {
            lock (_gate)
                return _queue.Count;
        }

        private static readonly DateTimeOffset UnixEpoch = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }
}
