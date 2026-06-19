using System;

namespace Hadal.Core.Network
{
    public sealed class GatewaySessionState : IGatewaySessionState
    {
        public bool IsHandshakeComplete { get; private set; }

        public event Action HandshakeCompleted;

        public void CompleteHandshake()
        {
            if (IsHandshakeComplete)
                return;

            IsHandshakeComplete = true;
            HandshakeCompleted?.Invoke();
        }

        public void Reset()
        {
            IsHandshakeComplete = false;
        }
    }
}
