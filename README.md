# Fabrik.SimpleBus

## Overview

Fabrik.SimpleBus is a simple in-process message bus written in C# that uses TPL Dataflow to provide asynchronous messaging capabilities.

This is useful when you are handling messages asynchronously or need to fire-and-forget.

## Getting Started

#### Creating a new message bus instance:

	IBus bus = new InProcessBus();

#### Subscribing with a delegate

Both synchronous and asynchronous delegates are supported.

    bus.Subscribe<string>(message => Console.WriteLine("Received message: {0}", message));
    bus.Subscribe<string>(async message => await WriteMessageAsync(message));

#### Strongly typed message handlers

    public class Message
    {
        public string Body { get; set; }
    }

    public class MessageHandler : IHandle<Message>
    {
        public void Handle(Message message)
        {
            Console.WriteLine("{0} handled {1}", this.GetType().Name, typeof(Message).Name);
        }
    }

    public class AsyncMessageHandler : IHandleAsync<Message>
    {
        public async Task HandleAsync(Message message)
        {
            await Task.Delay(1000);
            Console.WriteLine("{0} handled {1}", this.GetType().Name, typeof(Message).Name);
        }
    }

**Note: Messages are matched to handlers based on whether they are or implement the required message type. 
This makes it possible to subscribe to all messages of a specific base class or interface.**

#### Subscribing message handlers:

In most cases you will want to initialize a handler *per message*, for example, you may need to construct the handler using your IoC tool.

`IBus.Subscribe` provides overloads for passing in a `Func<IHandle>` and `Func<IHandlAsync>` for this purpose:

    bus.Subscribe<Message>(() => new MessageHandler());
    bus.Subscribe<Message>(() => new AsyncMessageHandler());

Using StructureMap we may do something like this:

	bus.Subscribe<Message>(ObjectFactory.GetInstance<MessageHandler>);

#### Unsubscribing

The `IBus.Subscribe` method returns a `Guid` subscription identifier. You can use this to unsubscribe at runtime:

    var id = bus.Subscribe<string>(message => Console.WriteLine(message));
    bus.Unsubscribe(id);

Note that unsubscribing a handler will not effect any messages currently being processed.

#### Sending messages

    bus.SendAsync(input).ContinueWith(task => Console.WriteLine("Enter another message")).Wait();



## License

Licensed under the MIT License.


