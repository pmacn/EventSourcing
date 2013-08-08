using NUnit.Framework;
using System.Runtime.Serialization;

namespace EventSourcing.Serialization.Json.Tests
{
    [TestFixture]
    public class JsonEventSerializerTests
    {
        private JsonEventSerializer _serializer;

        private TestEvent _event;

        [SetUp]
        public void TestSetup()
        {
            _event = new TestEvent(new TestId(1), "testing");
        }

        [Test]
        public void EventSerializesAndDeserializes()
        {
            // This runs terribly slow but subsequent serializations/deserializations are much quicker.
            _serializer = new JsonEventSerializer();
            var jsonByteArray = _serializer.Serialize(_event);
            var deserialized = (TestEvent)_serializer.Deserialize(jsonByteArray);
            Assert.AreEqual(_event.Id, deserialized.Id);
            Assert.AreEqual(_event.TestString, deserialized.TestString);
        }
    }

    [DataContract(Namespace="TestingContractNamespace")]
    public class TestEvent : IEvent<TestId>
    {
        [DataMember(Order=1)]
        public TestId Id { get; private set; }

        [DataMember(Order=2)]
        public string TestString { get; private set; }

        public TestEvent() { }

        public TestEvent(TestId id, string testString)
        {
            Id = id;
            TestString = testString;
        }
    }

    public class TestId : AbstractAggregateIdentity<int>
    {
        public TestId(int id)
        {
            Id = id;
        }

        public override sealed int Id { get ; protected set; }

        public override string GetTag() { return "Test"; }
    }
}
