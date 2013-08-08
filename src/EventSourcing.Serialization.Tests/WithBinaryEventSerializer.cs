using NUnit.Framework;
using System;
using System.Runtime.Serialization;

namespace EventSourcing.Serialization.Tests
{
    [TestFixture]
    public class WithBinaryEventSerializer
    {
        readonly IEventSerializer _serializer = new BinaryEventSerializer();

        readonly TestEvent _event = new TestEvent(new TestId(1), "testing");

        [SetUp]
        public void TestSetup()
        {

        }

        [Test]
        public void EventSerializesAndDeserializes()
        {
            var eventData = _serializer.Serialize(_event);
            var deserialized = _serializer.Deserialize(eventData) as TestEvent;
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(_event.Id, deserialized.Id);
            Assert.AreEqual(_event.TestString, deserialized.TestString);
        }
    }

    [Serializable,
    DataContract(Namespace = "TestingContractNamespace", Name = "TestEvent")]
    public class TestEvent : IEvent<TestId>
    {
        [DataMember(Order = 1)]
        public TestId Id { get; private set; }

        [DataMember(Order = 2)]
        public string TestString { get; private set; }

        public TestEvent() { }

        public TestEvent(TestId id, string testString)
        {
            Id = id;
            TestString = testString;
        }
    }

    [Serializable]
    public class TestId : AbstractAggregateIdentity<int>
    {
        public TestId(int id)
        {
            Id = id;
        }

        public override sealed int Id { get; protected set; }

        public override string GetTag() { return "Test"; }
    }
}
