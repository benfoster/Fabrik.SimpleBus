using Fabrik.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fabrik.SimpleBus
{
    internal sealed class Subscription
    {
        public Guid Id { get; private set; }
        public Func<object, CancellationToken, Task> Handler { get; private set; }

        Subscription(Func<object, CancellationToken, Task> handler)
        {
            Ensure.Argument.NotNull(handler, "handler");
            Id = Guid.NewGuid();
            Handler = handler;
        }

        public static Subscription Create<TMessage>(Func<TMessage, CancellationToken, Task> handler)
        {
            Func<object, CancellationToken, Task> handlerWithCheck = async (message, cancellationToken) =>
            {
                if (message.GetType().Implements<TMessage>())
                {
                    await handler.Invoke((TMessage)message, cancellationToken);
                }
            };

            return new Subscription(handlerWithCheck);
        }
    }
}
