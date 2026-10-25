using System;
using System.Threading.Tasks;
using HADAL.Shared.Auth;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Serialization;
using Hadal.Core.Events;
using Hadal.Data.Config;
using UnityEngine;
using VContainer.Unity;

namespace Hadal.Core.Network
{
    /// <summary>
    /// WebSocket ingress, gateway handshake, connection health, and reconciliation enqueue.
    /// </summary>
    public sealed class NetworkStateReceiver : INetworkStateReceiver, IStartable, ITickable, IDisposable
    {
        private const float HealthCheckIntervalSeconds = 0.5f;
        private const float InitialReconnectDelaySeconds = 1f;
        private const float MaxReconnectDelaySeconds = 10f;

        private readonly IWebSocketClient _webSocket;
        private readonly NetworkSerializationLayer _serialization;
        private readonly ICommandReconciliationSystem _reconciliation;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IGatewaySessionState _session;
        private readonly ILocalEventBus _localBus;
        private readonly GameConfigSO _config;
        private readonly IAuthService _auth;
        private readonly IPatchService _patch;

        private bool _listening;
        private bool _reconnectingAnnounced;
        private float _healthTimer;
        private float _reconnectDelay = InitialReconnectDelaySeconds;
        private float _nextReconnectAt;

        private bool _connecting;
        private bool _patchChecked;
        private bool _blocked;
        private string _accessToken = string.Empty;

        public NetworkStateReceiver(
            IWebSocketClient webSocket,
            NetworkSerializationLayer serialization,
            ICommandReconciliationSystem reconciliation,
            ICommandDispatcher commandDispatcher,
            IGatewaySessionState session,
            ILocalEventBus localBus,
            GameConfigSO config,
            IAuthService auth,
            IPatchService patch)
        {
            _webSocket = webSocket;
            _serialization = serialization;
            _reconciliation = reconciliation;
            _commandDispatcher = commandDispatcher;
            _session = session;
            _localBus = localBus;
            _config = config;
            _auth = auth;
            _patch = patch;
        }

        public void Start()
        {
            StartListening();

            if (ShouldConnect)
                _ = ConnectAsync();
        }

        // The in-process stub server replaces the gateway entirely in local-simulation mode.
        private bool ShouldConnect =>
            !_blocked && _config != null && !_config.UseLocalSimulation && !string.IsNullOrWhiteSpace(_config.GatewayUrl);

        public void StartListening()
        {
            if (_listening)
                return;

            _webSocket.OnMessageReceived += HandleMessageReceived;
            _webSocket.OnConnected += HandleConnected;
            _webSocket.OnDisconnected += HandleDisconnected;
            _listening = true;
        }

        public void StopListening()
        {
            if (!_listening)
                return;

            _webSocket.OnMessageReceived -= HandleMessageReceived;
            _webSocket.OnConnected -= HandleConnected;
            _webSocket.OnDisconnected -= HandleDisconnected;
            _listening = false;
        }

        public void Tick()
        {
            _webSocket.DispatchMessageQueue();

            _healthTimer += Time.deltaTime;
            if (_healthTimer < HealthCheckIntervalSeconds)
                return;

            _healthTimer = 0f;
            EvaluateConnectionHealth();
        }

        public void Dispose()
        {
            StopListening();
        }

        /// <summary>
        /// Patch check (once per session) → access token → socket. One attempt at a time; failures
        /// fall back to the health loop's backoff.
        /// </summary>
        private async Task ConnectAsync()
        {
            if (_connecting || !ShouldConnect)
                return;

            _connecting = true;
            try
            {
                if (!_patchChecked)
                {
                    var (verdict, manifest) = await _patch.CheckAsync();
                    switch (verdict)
                    {
                        case PatchVerdict.UpdateRequired:
                            Block(manifest.minClientVersion, manifest.latestClientVersion, manifest.updateUrl);
                            return;

                        case PatchVerdict.Maintenance:
                            _localBus.Publish(new ServerMaintenanceEvent { Message = manifest.message });
                            _nextReconnectAt = Time.realtimeSinceStartup + MaxReconnectDelaySeconds;
                            return;
                    }

                    _patchChecked = true;
                    _localBus.Publish(new ServerAnnouncementEvent { Text = manifest.announcement ?? string.Empty });
                    if (PatchService.IsOlder(Application.version, manifest.latestClientVersion) && !string.IsNullOrEmpty(manifest.updateUrl))
                        _localBus.Publish(new ClientUpdateAvailableEvent { LatestVersion = manifest.latestClientVersion, Url = manifest.updateUrl });
                    await SyncCatalogSafelyAsync(manifest);
                }

                _accessToken = await _auth.GetAccessTokenAsync();
                _webSocket.Connect(_config.GatewayUrl);
            }
            catch (AccountBannedException)
            {
                Ban();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[NetworkStateReceiver] Connect attempt failed: {exception.Message}");
            }
            finally
            {
                _connecting = false;
            }
        }

        // The bundled catalog still matches what the server enforces closely enough to play;
        // a failed download must never keep the player out.
        private async Task SyncCatalogSafelyAsync(VersionManifest manifest)
        {
            try
            {
                await _patch.SyncCatalogAsync(manifest);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[NetworkStateReceiver] Live catalog sync failed: {exception.Message}");
            }
        }

        private void Ban()
        {
            _blocked = true;
            _localBus.Publish(new AccountBannedEvent());
            Debug.LogWarning("[NetworkStateReceiver] The account is suspended — not connecting.");
        }

        private void Block(string required, string latest, string url = "")
        {
            _blocked = true;
            _localBus.Publish(new ClientUpdateRequiredEvent { RequiredVersion = required, LatestVersion = latest, Url = url });
            Debug.LogWarning($"[NetworkStateReceiver] Client {Application.version} is below the required {required} — not connecting.");
        }

        private void HandleConnected()
        {
            _session.Reset();
            _reconnectDelay = InitialReconnectDelaySeconds;
            SendHandshakeCommand();
        }

        private void HandleDisconnected()
        {
            _session.Reset();

            // Frames queued before the drop must not land after the reconnect snapshot, and
            // commands sent into the void will never be acked — the snapshot is the truth now.
            _reconciliation.ResetForReconnect();
        }

        private void SendHandshakeCommand()
        {
            var handshake = new ClientAuthCommand
            {
                CommandId = Guid.NewGuid().ToString("N"),
                ClientSequence = 0,
                ClientSchemaVersion = SchemaVersion.Current,
                ClientVersion = Application.version,
                DeviceId = SystemInfo.deviceUniqueIdentifier,
                AccessToken = _accessToken
            };

            _commandDispatcher.DispatchHandshake(handshake);
        }

        private void EvaluateConnectionHealth()
        {
            if (_webSocket.IsConnected)
            {
                if (_reconnectingAnnounced)
                {
                    _localBus.Publish(new NetworkConnectedEvent());
                    _reconnectingAnnounced = false;
                }

                return;
            }

            if (!ShouldConnect || _connecting)
                return;

            if (!_reconnectingAnnounced)
            {
                _session.Reset();
                _localBus.Publish(new NetworkReconnectingEvent { Message = "Reconnecting..." });
                _reconnectingAnnounced = true;
                _nextReconnectAt = Mathf.Max(_nextReconnectAt, Time.realtimeSinceStartup + _reconnectDelay);
                return;
            }

            if (_webSocket.ConnectionState == WebSocketConnectionState.Connecting
                || Time.realtimeSinceStartup < _nextReconnectAt)
                return;

            Debug.Log($"[NetworkStateReceiver] Reconnecting to {_config.GatewayUrl} (retry in {_reconnectDelay:0}s if it fails).");
            _nextReconnectAt = Time.realtimeSinceStartup + _reconnectDelay;
            _reconnectDelay = Mathf.Min(_reconnectDelay * 2f, MaxReconnectDelaySeconds);
            _ = ConnectAsync();
        }

        private void HandleMessageReceived(byte[] wire)
        {
            if (wire == null || wire.Length == 0)
                return;

            // Every server frame is a ServerMessage — the inner envelopes are wire-compatible
            // with each other, so the field number in ServerMessage is the only safe discriminator.
            if (!_serialization.TryDeserialize(wire, out ServerMessage message) || message == null)
            {
                Debug.LogWarning($"[NetworkStateReceiver] Undecodable wire frame ({wire.Length} bytes).");
                return;
            }

            if (message.SchemaVersion < SchemaVersion.MinSupported)
            {
                Debug.LogWarning($"[NetworkStateReceiver] Server schema {message.SchemaVersion} below minimum {SchemaVersion.MinSupported}.");
                return;
            }

            switch (message.Kind)
            {
                case ServerMessageKind.HandshakeAck:
                    HandleHandshakeAck(message.HandshakeAck);
                    return;

                case ServerMessageKind.StateDelta:
                    if (TryDecodeDelta(message.Delta, out var deltaPacket))
                        _reconciliation.EnqueueInbound(deltaPacket);
                    return;

                case ServerMessageKind.StateSnapshot:
                    if (TryDecodeSnapshot(message.Snapshot, out var snapshotPacket))
                        _reconciliation.EnqueueInbound(snapshotPacket);
                    return;

                default:
                    Debug.LogWarning($"[NetworkStateReceiver] Unrecognized server message ({wire.Length} bytes).");
                    return;
            }
        }

        private void HandleHandshakeAck(HandshakeAckEnvelope ack)
        {
            if (ack == null || !ack.Accepted || ack.Result != CommandResultCode.Accepted)
            {
                Debug.LogWarning($"[NetworkStateReceiver] Gateway handshake rejected ({ack?.Result} {ack?.Reason}).");

                switch (ack?.Reason)
                {
                    case HandshakeRejectReasons.TokenExpired:
                    case HandshakeRejectReasons.TokenInvalid:
                    case HandshakeRejectReasons.TokenMissing:
                        // The server closes the socket; the next reconnect fetches a fresh token.
                        _auth?.InvalidateAccessToken();
                        break;

                    case HandshakeRejectReasons.SchemaIncompatible:
                        Block("(newer schema)", string.Empty);
                        break;

                    case HandshakeRejectReasons.Banned:
                        Ban();
                        break;
                }

                return;
            }

            _session.CompleteHandshake();
            _commandDispatcher.FlushBuffer();
            Debug.Log("[NetworkStateReceiver] Gateway handshake complete — command buffer flushed.");
        }

        private bool TryDecodeDelta(StateDeltaEnvelope envelope, out NetworkInboundPacket packet)
        {
            packet = null!;

            if (envelope.Payload == null || envelope.Payload.Length == 0)
                return false;

            if (!_serialization.TryDeserialize(envelope.Payload, out StateDelta delta))
            {
                Debug.LogWarning("[NetworkStateReceiver] StateDelta payload undecodable.");
                return false;
            }

            packet = new NetworkInboundPacket
            {
                Kind = NetworkInboundKind.StateDelta,
                SchemaVersion = envelope.SchemaVersion,
                ServerTick = envelope.ServerTick,
                BaselineTick = envelope.BaselineTick,
                Delta = delta
            };

            return true;
        }

        private bool TryDecodeSnapshot(StateSnapshotEnvelope envelope, out NetworkInboundPacket packet)
        {
            packet = null!;

            if (envelope.Payload == null || envelope.Payload.Length == 0)
                return false;

            if (!_serialization.TryDeserialize(envelope.Payload, out StateSnapshot snapshot))
            {
                Debug.LogWarning("[NetworkStateReceiver] StateSnapshot payload undecodable.");
                return false;
            }

            packet = new NetworkInboundPacket
            {
                Kind = NetworkInboundKind.StateSnapshot,
                SchemaVersion = envelope.SchemaVersion,
                ServerTick = envelope.ServerTick,
                Snapshot = snapshot
            };

            return true;
        }
    }
}
