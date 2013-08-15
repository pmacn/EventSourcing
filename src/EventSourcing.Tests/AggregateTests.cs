using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Runtime.Serialization;

namespace EventSourcing.Tests
{
    [TestFixture]
    public class AggregateTests
    {
        public TestId aggregateId;
        public TestAggregate aggregate;

        [SetUp]
        public void TestSetup()
        {
            aggregateId = new TestId(1234);
            aggregate = new TestAggregate();
            aggregate.Create(aggregateId);
            aggregate.UncommittedEvents.MarkAsCommitted();
        }

        [Test]
        public void Monkey()
        {
            const string text = "test";
            const int entityId = 1;
            aggregate.AddEntity(entityId, text);

            Assert.IsInstanceOf<EntityAdded>(aggregate.UncommittedEvents.Single());
        }

        [Serializable]
        public sealed class TestId : AbstractAggregateIdentity<int>
        {
            public TestId(int id)
            {
                Id = id;
            }

            public override int Id { get; protected set; }

            public override string GetTag() { return "Test"; }
        }

        public sealed class TestAggregate : AggregateRootEntity<TestId>
        {
            private readonly List<TestEntity> _entities = new List<TestEntity>();

            public TestAggregate()
                : this(new ConfigurationEventRouter())
            { }

            private TestAggregate(ConfigurationEventRouter eventRouter)
                : base(eventRouter)
            {
                eventRouter.Register<EntityAdded>(When);
                eventRouter.Register<AggregateCreated>(When);
            }

            public void Create(TestId id)
            {
                ApplyChange(new AggregateCreated(id));
            }

            public void AddEntity(int entityId, string text)
            {
                ApplyChange(new EntityAdded(Id, entityId, text));
            }

            #region EventHandlers

            private void When(AggregateCreated e)
            {
                Id = e.Id;
            }

            private void When(EntityAdded e)
            {
                var entity = new TestEntity(ApplyChange);
                entity.Apply(e);
                _entities.Add(entity);
            }

            #endregion
        }

        public sealed class TestEntity : Entity
        {
            public TestEntity(Action<IEvent> registerEvent) : this(new ConventionEventRouter(), registerEvent) { }

            private TestEntity(ConventionEventRouter eventRouter, Action<IEvent> registerEvent)
                : base(eventRouter, registerEvent)
            {
                eventRouter.Register(this);
            }

            public int Id { get; private set; }

            public string Text { get; private set; }

            #region EventHandlers

            private void When(EntityAdded e)
            {
                Id = e.EntityId;
                Text = e.Text;
            }

            #endregion
        }

        [Serializable,
        DataContract(Namespace = "TestContractNamespace")]
        public class AggregateCreated : IEvent<TestId>
        {
            public AggregateCreated(TestId id)
            {
                Id = id;
            }

            [DataMember(Order = 1)] public TestId Id { get; private set; }
        }

        [Serializable]
        [DataContract(Namespace = "TestContractNamespace")]
        public class EntityAdded : IEvent<TestId>
        {
            public EntityAdded(TestId aggregateId, int entityId, string text)
            {
                Id = aggregateId;
                EntityId = entityId;
                Text = text;
            }

            [DataMember(Order = 1)] public TestId Id { get; private set; }
            [DataMember(Order = 2)] public int EntityId { get; private set; }
            [DataMember(Order = 3)] public string Text { get; private set; }
        }
    }
}
