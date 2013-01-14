using Fabrik.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Fabrik.SimpleBus
{
    public class InProcessBus : IBus
    {
        private readonly ConcurrentQueue<Subscription> subscriptionRequests = new ConcurrentQueue<Subscription>();
        private readonly ConcurrentQueue<Guid> unsubscribeRequests = new ConcurrentQueue<Guid>();
        private readonly ActionBlock<SendMessageRequest> messageProcessor;

        public InProcessBus()
        {
            // Only ever accessed from (single threaded) ActionBlock, so it is thread safe
            var subscriptions = new List<Subscription>();

            messageProcessor = new ActionBlock<SendMessageRequest>(async request =>
            {
                // Process unsubscribe requests
                Guid subscriptionId;
                while (unsubscribeRequests.TryDequeue(out subscriptionId))
                {
                    Trace.TraceInformation("Removing subscription '{0}'.".FormatWith(subscriptionId));
                    subscriptions.RemoveAll(s => s.Id == subscriptionId);
                }

                // Process subscribe requests
                Subscription newSubscription;
                while (subscriptionRequests.TryDequeue(out newSubscription))
                {
                    Trace.TraceInformation("Adding subscription '{0}'.".FormatWith(newSubscription.Id));
                    subscriptions.Add(newSubscription);
                }

                var result = true;

                Trace.TraceInformation("Processing message type '{0}'.".FormatWith(request.Payload.GetType().FullName));

                foreach (var subscription in subscriptions)
                {
                    if (request.CancellationToken.IsCancellationRequested)
                    {
                        Trace.TraceWarning("Cancellation request recieved. Processing stopped.");
                        result = false;
                        break;
                    }
                    
                    try
                    {
                        Trace.TraceInformation("Executing subscription '{0}' handler.".FormatWith(subscription.Id));
                        await subscription.Handler.Invoke(request.Payload, request.CancellationToken);
                    }
                    catch (Exception ex)
                    {                        
                        Trace.TraceError("There was a problem executing subscription '{0}' handler. Exception message: {1}".FormatWith(subscription.Id, ex.Message));
                        result = false;
                        continue;
                    }
                }

                // All done send result back to caller
                request.OnSendComplete(result);
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
            messageProcessor.Post(new SendMessageRequest(message, cancellationToken, result => tcs.SetResult(result)));
            return tcs.Task;
        }

        public Guid Subscribe<TMessage>(Action<TMessage> handler)
        {
            return Subscribe<TMessage>((message, cancellationToken) =>
            {
                handler.Invoke(message);
                return Task.FromResult(0);
            });
        }

        public Guid Subscribe<TMessage>(Func<TMessage, CancellationToken, Task> handler)
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
