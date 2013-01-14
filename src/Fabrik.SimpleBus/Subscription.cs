using Fabrik.Common;
using System;
using System.Threading.Tasks;

namespace Fabrik.SimpleBus
{
    internal sealed class Subscription
    {
        public Guid Id { get; private set; }
        public Func<object, Task> Handler { get; private set; }

        Subscription(Func<object, Task> handler)
        {
            Ensure.Argument.NotNull(handler, "handler");
            Id = Guid.NewGuid();
            Handler = handler;
        }

        public static Subscription Create<TMessage>(Func<TMessage, Task> handler)
        {
            Func<object, Task> handlerWithCheck = async message =>
            {
                if (message.GetType().Implements<TMessage>())
                {
                    await handler.Invoke((TMessage)message);
                }
            };

            return new Subscription(handlerWithCheck);
        }
    }
}
