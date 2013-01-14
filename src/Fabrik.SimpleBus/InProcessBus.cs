using Fabrik.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Fabrik.SimpleBus
{
    public class InProcessBus : IBus
    {
        private readonly ConcurrentQueue<Subscription> subscriptionRequests = new ConcurrentQueue<Subscription>();
        private readonly ConcurrentQueue<Guid> unsubscribeRequests = new ConcurrentQueue<Guid>();
        private readonly ActionBlock<Tuple<object, Action>> messageProcessor;

        public InProcessBus()
        {
            // Only ever accessed from (single threaded) ActionBlock, so it is thread safe
            var subscriptions = new List<Subscription>();

            messageProcessor = new ActionBlock<Tuple<object, Action>>(async tuple =>
            {
                var message = tuple.Item1;
                var completionAction = tuple.Item2;

                // Process unsubscribe requests
                Guid subscriptionId;
                while (unsubscribeRequests.TryDequeue(out subscriptionId))
                {
                    subscriptions.RemoveAll(s => s.Id == subscriptionId);
                }

                // Process subscribe requests
                Subscription subscription;
                while (subscriptionRequests.TryDequeue(out subscription))
                {
                    subscriptions.Add(subscription);
                }

                // Execute all handlers
                await Task.WhenAll(subscriptions.Select(s => s.Handler.Invoke(message)));

                completionAction();
            });
        }
               
        public Task SendAsync<TMessage>(TMessage message)
        {
            return SendAsync(message, CancellationToken.None);
        }

        public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        {
            Ensure.Argument.NotNull(message, "message");
            Ensure.Argument.NotNull(cancellationToken, "cancellationToken");

            var tcs = new TaskCompletionSource<bool>();
            Action onCompleted = () => tcs.SetResult(true);

            messageProcessor.Post(new Tuple<object, Action>(message, onCompleted));
            return tcs.Task;
        }

        public Guid Subscribe<TMessage>(Action<TMessage> handler)
        {
            return Subscribe<TMessage>(message =>
            {
                handler.Invoke(message);
                return Task.FromResult(0);
            });
        }

        public Guid Subscribe<TMessage>(Func<TMessage, Task> handler)
        {
            Ensure.Argument.NotNull(handler, "handler");
            var subscription = Subscription.Create<TMessage>(handler);
            subscriptionRequests.Enqueue(subscription);
            return subscription.Id;
        }

        public void Unsubscribe(Guid subscriptionId)
        {
            unsubscribeRequests.Enqueue(subscriptionId);
        }
    }
}
