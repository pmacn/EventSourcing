using EventSourcing.ApplicationService;
using EventSourcing.Persistence;
using EventSourcing.Serialization;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EventSourcing.Example
{
    /// <summary>
    /// 10000ft tests
    /// </summary>
    [TestFixture]
    public class AggregateWithoutStateObject
    {
        private MemoryCommandQueue _commandQueue;

        private TestEventPublisher _eventPublisher;

        private ApplicationService<ExampleId> _appService;

        private DefaultApplicationServiceHost _serviceHost;

        private TestDomainErrorRouter _errorRouter;

        [SetUp]
        public void TestSetup()
        {
            _eventPublisher = new TestEventPublisher();
            var conflictDetector = new DelegateConflictDetector();
            var eventStore = new EventStore(new MemoryEventPersistance(new BinaryEventSerializer()), _eventPublisher, conflictDetector);
            var repo = new Repository(eventStore);
            _commandQueue = new MemoryCommandQueue();
            _errorRouter = new TestDomainErrorRouter();
            _appService = new ExampleApplicationService(repo, _errorRouter);
            _serviceHost = new DefaultApplicationServiceHost(_commandQueue);
            _serviceHost.LoadService(_appService);
            _serviceHost.Start();
        }

        [TearDown]
        public void TestTeardown() { _serviceHost.Stop(); }

        [Test]
        public void EnqueingCommandWillPublishExpectedEvent()
        {
            var id = new ExampleId(1);
            var cmd = new OpenExample(id);
            _commandQueue.Enqueue(cmd);
            Thread.Sleep(100); // Yay, threading hackery~
            var e = _eventPublisher.PublishedEvents.FirstOrDefault();
            Assert.IsInstanceOf(typeof(ExampleOpened), e);
        }

        [Test]
        public void DomainErrorsAreRouted()
        {
            var id = new ExampleId(1);
            var cmd = new OpenExample(id);
            _commandQueue.Enqueue(cmd);
            _commandQueue.Enqueue(cmd);
            Thread.Sleep(100);
            CollectionAssert.IsNotEmpty(_errorRouter.RoutedErrors);
        }
    }

    public class AggregateWithStateObject
    {
        private MemoryCommandQueue _commandQueue;

        private TestEventPublisher _eventPublisher;

        private ApplicationService<ExampleId> _appService;

        private DefaultApplicationServiceHost _serviceHost;

        private TestDomainErrorRouter _errorRouter;

        [SetUp]
        public void TestSetup()
        {
            _eventPublisher = new TestEventPublisher();
            var conflictDetector = new DelegateConflictDetector();
            var eventStore = new EventStore(new MemoryEventPersistance(new BinaryEventSerializer()), _eventPublisher, conflictDetector);
            var repo = new Repository(eventStore);
            _commandQueue = new MemoryCommandQueue();
            _errorRouter = new TestDomainErrorRouter();
            _appService = new ExampleWithStateApplicationService(repo, _errorRouter);
            _serviceHost = new DefaultApplicationServiceHost(_commandQueue);
            _serviceHost.LoadService(_appService);
            _serviceHost.Start();
        }

        [TearDown]
        public void TestTeardown() { _serviceHost.Stop(); }

        [Test]
        public void EnqueingCommandWillPublishExpectedEvent()
        {
            var id = new ExampleId(1);
            var cmd = new OpenExample(id);
            _commandQueue.Enqueue(cmd);
            Thread.Sleep(100); // Yay, threading hackery~
            var e = _eventPublisher.PublishedEvents.FirstOrDefault();
            Assert.IsInstanceOf(typeof(ExampleOpened), e);
        }
    }

    public class TestEventPublisher : IEventPublisher
    {
        public List<IEvent> PublishedEvents = new List<IEvent>();

        public void Publish<TEvent>(TEvent eventToPublish) where TEvent : class, IEvent
        {
            PublishedEvents.Add(eventToPublish);
        }

        public void Publish<TEvent>(TEvent[] eventsToPublish) where TEvent : class, IEvent
        {
            PublishedEvents.AddRange(eventsToPublish);
        }
    }

    public class TestDomainErrorRouter : IDomainErrorRouter
    {
        public List<DomainError> RoutedErrors = new List<DomainError>();

        public void Route(DomainError error)
        {
            RoutedErrors.Add(error);
        }
    }
}