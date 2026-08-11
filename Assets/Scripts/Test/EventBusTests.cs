using Core;
using NUnit.Framework;

namespace Test
{
    public class EventBusTests
    {
        public class TestEvent
        {
            public string Name;
        }

        private bool _handlerACalled;
        private bool _handlerBCalled;
        private TestEvent _receivedByA;
        private TestEvent _receivedByB;

        [SetUp]
        public void SetUp()
        {
            _handlerACalled = false;
            _handlerBCalled = false;
            _receivedByA = null;
            _receivedByB = null;

            // 清场：上一条测试订阅过的方法，退订掉，防止串状态
            EventBus<TestEvent>.Unsubscribe(HandlerA);
            EventBus<TestEvent>.Unsubscribe(HandlerB);
        }

        private void HandlerA(TestEvent e)
        {
            _handlerACalled = true;
            _receivedByA = e;
        }

        private void HandlerB(TestEvent e)
        {
            _handlerBCalled = true;
            _receivedByB = e;
        }

        [Test]
        public void Publish_NotifiesAllSubscribers()
        {
            EventBus<TestEvent>.Subscribe(HandlerA);
            EventBus<TestEvent>.Subscribe(HandlerB);

            var e = new TestEvent { Name = "Leo" };
            EventBus<TestEvent>.Publish(e);

            Assert.IsTrue(_handlerACalled);
            Assert.IsTrue(_handlerBCalled);
        }

        [Test]
        public void Publish_SendsSameInstanceToAllSubscribers()
        {
            EventBus<TestEvent>.Subscribe(HandlerA);
            EventBus<TestEvent>.Subscribe(HandlerB);

            var e = new TestEvent { Name = "Leo" };
            EventBus<TestEvent>.Publish(e);

            Assert.AreSame(e, _receivedByA);
            Assert.AreSame(e, _receivedByB);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            EventBus<TestEvent>.Subscribe(HandlerA);
            EventBus<TestEvent>.Subscribe(HandlerB);
            EventBus<TestEvent>.Unsubscribe(HandlerA);

            EventBus<TestEvent>.Publish(new TestEvent { Name = "Kai" });

            Assert.IsFalse(_handlerACalled);
            Assert.IsTrue(_handlerBCalled);
        }
    }
}
