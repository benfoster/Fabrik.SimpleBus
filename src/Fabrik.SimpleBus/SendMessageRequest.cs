using Fabrik.Common;
using System;
using System.Threading;

namespace Fabrik.SimpleBus
{
    internal sealed class SendMessageRequest
    {
        public object Payload { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public Action<bool> OnSendComplete { get; private set; }

        public SendMessageRequest(object payload, CancellationToken cancellationToken)
            : this(payload, cancellationToken, success => { }) { }

        public SendMessageRequest(object payload, CancellationToken cancellationToken, Action<bool> onSendComplete)
        {
            Ensure.Argument.NotNull(payload, "payload");
            Ensure.Argument.NotNull(cancellationToken, "cancellationToken");
            Ensure.Argument.NotNull(onSendComplete, "onSendComplete");

            Payload = payload;
            CancellationToken = cancellationToken;
            OnSendComplete = onSendComplete;
        }
    }
}
