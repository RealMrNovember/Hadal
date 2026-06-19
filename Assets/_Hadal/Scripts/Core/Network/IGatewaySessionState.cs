using System;

namespace Hadal.Core.Network
{
    public interface IGatewaySessionState
    {
        bool IsHandshakeComplete { get; }

        event Action HandshakeCompleted;

        void CompleteHandshake();

        void Reset();
    }
}
