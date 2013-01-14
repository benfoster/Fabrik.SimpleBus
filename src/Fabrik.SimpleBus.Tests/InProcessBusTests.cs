using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fabrik.SimpleBus.Tests
{
    [TestFixture]
    public class InProcessBusTests
    {
        IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = new InProcessBus();
        }

        [Test]
        public async void Can_send_messages_withut_any_subscribers()
        {
            await bus.SendAsync("test");
        }

        [Test]
        public async void Can_subscribe_to_simple_types()
        {
            var handler = new SyncMessageHandler<string>();
            bus.Subscribe<string>(handler.Handle);
            await bus.SendAsync("test");
            Assert.IsTrue(handler.WasInvoked);
        }

        [Test]
        public async void Can_subscribe_to_complex_types()
        {
            var handler = new SyncMessageHandler<TestMessage>();
            bus.Subscribe<TestMessage>(handler.Handle);
            await bus.SendAsync(new TestMessage());
            Assert.IsTrue(handler.WasInvoked);
        }

        [Test]
        public async void Should_invoke_all_handlers_that_implement_the_message_type()
        {
            var h1 = new SyncMessageHandler<IMessage>();
            var h2 = new SyncMessageHandler<BaseMessage>();
            var h3 = new SyncMessageHandler<TestMessage>();
            var h4 = new SyncMessageHandler<TestMessage>();

            bus.Subscribe<IMessage>(h1.Handle);
            bus.Subscribe<BaseMessage>(h2.Handle);
            bus.Subscribe<TestMessage>(h3.Handle);
            bus.Subscribe<TestMessage>(h4.Handle);

            await bus.SendAsync(new TestMessage());

            Assert.IsTrue(h1.WasInvoked);
            Assert.IsTrue(h2.WasInvoked);
            Assert.IsTrue(h3.WasInvoked);
            Assert.IsTrue(h4.WasInvoked);
        }

        [Test]
        public async void Can_unsubscribe_an_existing_handler()
        {
            var handler = new SyncMessageHandler<string>();
            var id = bus.Subscribe<String>(handler.Handle);
            await bus.SendAsync("test");

            Assert.AreEqual(1, handler.InvocationCount);

            bus.Unsubscribe(id);
            await bus.SendAsync("test 2");

            // Should not have been invoked again
            Assert.AreEqual(1, handler.InvocationCount);
        }

        [Test]
        public async void Should_invoke_message_handler_factories_per_message()
        {
            SyncMessageHandler<string> handler = null;
            bus.Subscribe(() => handler = new SyncMessageHandler<string>());

            await bus.SendAsync("test");
            await bus.SendAsync("test 2");

            // Should have been recreated on second message
            Assert.AreEqual(1, handler.InvocationCount);
        }

        [Test]
        public async void Should_execute_remaining_handlers_on_exception()
        {
            var h1 = new Action<string>(x => { throw new Exception(); });
            var h2 = new SyncMessageHandler<string>();
            var h3 = new SyncMessageHandler<string>();

            bus.Subscribe<string>(h1);
            bus.Subscribe<string>(h2.Handle);
            bus.Subscribe<string>(h3.Handle);

            await bus.SendAsync("test");

            Assert.IsTrue(h2.WasInvoked);
            Assert.IsTrue(h3.WasInvoked);
        }

        [Test]
        public void Cancelling_a_message_should_stop_all_remaining_handlers_from_executing()
        {
            var h1 = new SyncMessageHandler<string>();
            var h2 = new AsyncMessageHandler<string>();
            var h3 = new AsyncMessageHandler<string>();

            bus.Subscribe<string>(h1.Handle);
            bus.Subscribe<string>(h2.HandleAsync);
            bus.Subscribe<string>(h3.HandleAsync);

            var cts = new CancellationTokenSource(millisecondsDelay: 1000); // h2 takes 2000ms to complete
            bus.SendAsync("test", cts.Token).Wait();

            Assert.IsTrue(h1.WasInvoked);
            Assert.IsTrue(h2.WasInvoked);
            Assert.IsFalse(h3.WasInvoked);
        }

        private class TestMessage : BaseMessage, IMessage
        {

        }

        private abstract class BaseMessage
        {

        }

        private interface IMessage
        {

        }

        private class SyncMessageHandler<T> : IHandle<T>
        {
            int invocationCount = 0;

            public void Handle(T message)
            {
                invocationCount++;
            }

            public int InvocationCount { get { return invocationCount; } }
            public bool WasInvoked { get { return invocationCount > 0; } }
        }

        private class AsyncMessageHandler<T> : SyncMessageHandler<T>, IHandleAsync<T>
        {
            public async Task HandleAsync(T message, CancellationToken cancellationToken)
            {
                await Task.Delay(2000);
                Handle(message);
            }
        }
    }
}
