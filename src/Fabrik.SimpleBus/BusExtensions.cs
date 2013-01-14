using System;
using System.Threading;

namespace Fabrik.SimpleBus
{
    public static class BusExtensions
    {
        public static Guid Subscribe<TMessage>(this IBus bus, Func<IHandle<TMessage>> handlerFactory)
        {
            return bus.Subscribe<TMessage>(message 
                => handlerFactory.Invoke().Handle(message));
        }
       
        public static Guid Subscribe<TMessage>(this IBus bus, Func<IHandleAsync<TMessage>> handlerFactory)
        {
            return bus.Subscribe<TMessage>((message, cancellationToken) 
                => handlerFactory.Invoke().HandleAsync(message, cancellationToken));
        }       
    }
}
