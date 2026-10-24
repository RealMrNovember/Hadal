using System;
using System.Collections.Generic;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Serialization;
using Hadal.Core.Events;
using Hadal.Core.Network;
using Hadal.Data.Config;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NetworkSerializationLayer = Hadal.Core.Network.NetworkSerializationLayer;

namespace Hadal.Tests.EditMode
{
    /// <summary>
    /// QA R2 / S8: inbound frames must be classified by the ServerMessage body, never by
    /// "whichever envelope happens to decode first".
    /// </summary>
    public sealed class ServerMessageRoutingTests
    {
        private readonly ProtobufNetNetworkSerializer _serializer = new();

        private FakeSocket _socket;
        private FakeSession _session;
        private FakeReconciliation _reconciliation;
        private GameConfigSO _config;
        private NetworkStateReceiver _receiver;

        [SetUp]
        public void SetUp()
        {
            _socket = new FakeSocket();
            _session = new FakeSession();
            _reconciliation = new FakeReconciliation();
            _config = ScriptableObject.CreateInstance<GameConfigSO>();
            _receiver = new NetworkStateReceiver(_socket, new NetworkSerializationLayer(), _reconciliation,
                new FakeDispatcher(), _session, new FakeBus(), _config);
            _receiver.StartListening();
        }

        [TearDown]
        public void TearDown()
        {
            _receiver.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void BareV1Envelopes_AreWireCompatible_WhichIsWhyServerMessageExists()
        {
            // Documents the original bug: a bare delta decodes as an *accepted* handshake ack.
            var ack = _serializer.Deserialize<HandshakeAckEnvelope>(_serializer.Serialize(Delta(1)));
            Assert.IsTrue(ack.Accepted);
        }

        [Test]
        public void DeltaWithTickOne_IsRoutedAsDelta_NotHandshake()
        {
            _socket.Receive(Wire(EnvelopeBuilder.WrapServerMessage(Delta(1))));

            Assert.AreEqual(1, _reconciliation.Packets.Count);
            Assert.AreEqual(NetworkInboundKind.StateDelta, _reconciliation.Packets[0].Kind);
            Assert.AreEqual(1UL, _reconciliation.Packets[0].ServerTick);
            Assert.IsFalse(_session.IsHandshakeComplete);
        }

        [Test]
        public void Snapshot_IsRoutedAsSnapshot()
        {
            var snapshot = new StateSnapshot { ServerTick = 7, PlayerId = "p1" };
            _socket.Receive(Wire(EnvelopeBuilder.WrapServerMessage(
                EnvelopeBuilder.WrapStateSnapshot(snapshot, _serializer.Serialize(snapshot)))));

            Assert.AreEqual(1, _reconciliation.Packets.Count);
            Assert.AreEqual(NetworkInboundKind.StateSnapshot, _reconciliation.Packets[0].Kind);
            Assert.AreEqual("p1", _reconciliation.Packets[0].Snapshot.PlayerId);
        }

        [Test]
        public void AcceptedHandshakeAck_CompletesSession()
        {
            _socket.Receive(Wire(EnvelopeBuilder.WrapServerMessage(Ack(CommandResultCode.Accepted, true))));

            Assert.IsTrue(_session.IsHandshakeComplete);
            Assert.IsEmpty(_reconciliation.Packets);
        }

        [Test]
        public void SchemaIncompatibleAck_DoesNotCompleteSession()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("handshake rejected"));
            _socket.Receive(Wire(EnvelopeBuilder.WrapServerMessage(Ack(CommandResultCode.SchemaIncompatible, false))));

            Assert.IsFalse(_session.IsHandshakeComplete);
        }

        [Test]
        public void BareEnvelope_IsDropped()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Undecodable wire frame"));
            _socket.Receive(_serializer.Serialize(Delta(1)));

            Assert.IsEmpty(_reconciliation.Packets);
            Assert.IsFalse(_session.IsHandshakeComplete);
        }

        [Test]
        public void MessageWithTwoBodies_IsDropped()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Unrecognized server message"));
            _socket.Receive(Wire(new ServerMessage
            {
                SchemaVersion = SchemaVersion.Current,
                HandshakeAck = Ack(CommandResultCode.Accepted, true),
                Delta = Delta(2)
            }));

            Assert.IsEmpty(_reconciliation.Packets);
            Assert.IsFalse(_session.IsHandshakeComplete);
        }

        [Test]
        public void SchemaBelowMinimum_IsDropped()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("below minimum"));
            _socket.Receive(Wire(new ServerMessage { SchemaVersion = SchemaVersion.MinSupported - 1, Delta = Delta(3) }));

            Assert.IsEmpty(_reconciliation.Packets);
        }

        [Test]
        public void CorruptDeltaPayload_IsDroppedWithoutThrowing()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("StateDelta payload undecodable"));
            var envelope = new StateDeltaEnvelope
            {
                SchemaVersion = SchemaVersion.Current,
                ServerTick = 5,
                Payload = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }
            };

            Assert.DoesNotThrow(() => _socket.Receive(Wire(EnvelopeBuilder.WrapServerMessage(envelope))));
            Assert.IsEmpty(_reconciliation.Packets);
        }

        private byte[] Wire(ServerMessage message) => _serializer.Serialize(message);

        private StateDeltaEnvelope Delta(ulong tick)
        {
            var delta = new StateDelta { ServerTick = tick };
            return EnvelopeBuilder.WrapStateDelta(delta, _serializer.Serialize(delta));
        }

        private static HandshakeAckEnvelope Ack(CommandResultCode result, bool accepted) => new()
        {
            SchemaVersion = SchemaVersion.Current,
            Accepted = accepted,
            Result = result
        };

        private sealed class FakeSocket : IWebSocketClient
        {
            public bool IsConnected => true;
            public WebSocketConnectionState ConnectionState => WebSocketConnectionState.Open;
            public event Action OnConnected { add { } remove { } }
            public event Action OnDisconnected { add { } remove { } }
            public event Action<byte[]> OnMessageReceived;
            public void Connect(string url) { }
            public void Send(byte[] data) { }
            public void Disconnect() { }
            public void DispatchMessageQueue() { }
            public void Receive(byte[] wire) => OnMessageReceived?.Invoke(wire);
        }

        private sealed class FakeSession : IGatewaySessionState
        {
            public bool IsHandshakeComplete { get; private set; }
            public event Action HandshakeCompleted;
            public void CompleteHandshake() { IsHandshakeComplete = true; HandshakeCompleted?.Invoke(); }
            public void Reset() => IsHandshakeComplete = false;
        }

        private sealed class FakeReconciliation : ICommandReconciliationSystem
        {
            public List<NetworkInboundPacket> Packets { get; } = new();
            public void EnqueueInbound(NetworkInboundPacket packet) => Packets.Add(packet);
            public void RegisterPendingCommand(string commandId, ulong clientSequence, CommandType commandType) { }
            public void ProcessQueue() { }
        }

        private sealed class FakeDispatcher : ICommandDispatcher
        {
            public void Dispatch<T>(T command) where T : ICommand { }
            public void DispatchHandshake<T>(T command) where T : ICommand { }
            public void FlushBuffer() { }
        }

        private sealed class FakeBus : ILocalEventBus
        {
            public void Subscribe<T>(Action<T> handler) { }
            public void Unsubscribe<T>(Action<T> handler) { }
            public void Publish<T>(T payload) { }
        }
    }
}
