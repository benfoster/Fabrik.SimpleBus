using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fabrik.SimpleBus
{
    public interface IBus
    {
        Task SendAsync<TMessage>(TMessage message);
        Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken);
        Guid Subscribe<TMessage>(Action<TMessage> handler);
        Guid Subscribe<TMessage>(Func<TMessage, CancellationToken, Task> handler);
        void Unsubscribe(Guid subscriptionId);
    }
}
