using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fabrik.SimpleBus.Demo
{
    class Program
    {       
        static void Main(string[] args)
        {
            new Program().Run();
        }

        private void Run()
        {           
            var bus = new InProcessBus();

            // Delegate Subscription
            bus.Subscribe<string>(message => Console.WriteLine("Received simple message: {0}", message));
            bus.Subscribe<string>(async message => await WriteMessageAsync(message));
            
            // Handler factory
            bus.Subscribe<Message>(() => new MessageHandler());

            // Async Handler Factory
            bus.Subscribe<Message>(() => new AsyncMessageHandler());

            string input;
            while ((input = Console.ReadLine()) != "q")
            {
                var t1 = bus.SendAsync(new Message { Body = input });
                var t2 = bus.SendAsync(input).ContinueWith(task => Console.WriteLine("Enter another message"));

                Task.WaitAll(t1, t2);
            }
        }

        private Task WriteMessageAsync(string message)
        {
            return Task.Delay(2000).ContinueWith(task => Console.WriteLine("Received simple message async: {0}", message));
        }
    }

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
        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            await Task.Delay(1000);
            Console.WriteLine("{0} handled {1}", this.GetType().Name, typeof(Message).Name);
        }
    }
}
