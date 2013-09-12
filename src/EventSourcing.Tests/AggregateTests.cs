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

            aggregate.UncommittedEvents.MarkAsCommitted();
            aggregate.UpdateEntityText(1, "new entity text");
            Assert.IsInstanceOf<EntityTextUpdated>(aggregate.UncommittedEvents.Single());
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
                eventRouter.Register<EntityTextUpdated>(When);
            }

            public void Create(TestId id)
            {
                ApplyChange(new AggregateCreated(id));
            }

            public void AddEntity(int entityId, string text)
            {
                // It's a little awkward having these checks here rather than in the TestEntity
                // But feels better than the alternative for now.
                if (entityId < 0)
                    throw DomainError.Named("invalid-entity-id", "Entity id cannot be negative");
                if (String.IsNullOrWhiteSpace(text))
                    throw DomainError.Named("entity-text-validation", "Entity text cannot be null, empty or whitespace");
                if(_entities.Any(e => e.Id == entityId))
                    throw DomainError.Named("entity-already-exists", String.Format("TestAggregate {0} already has an entity with id {1}", Id, entityId));

                ApplyChange(new EntityAdded(Id, entityId, text));
            }

            public void UpdateEntityText(int entityId, string updatedText)
            {
                var entity = _entities.SingleOrDefault(e => e.Id == entityId);
                if (entity == null)
                    throw DomainError.Named("no-such-entity", String.Format("Could not find entity with id {0} in TestAggregate {1}", entityId, Id));

                entity.UpdateText(updatedText);
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

            private void When(EntityTextUpdated e)
            {
                var entity = _entities.Single(x => x.Id == e.EntityId);
                entity.Apply(e);
            }

            #endregion
        }

        [Serializable]
        public class TestEntity : Entity
        {
            private TestId _testId;

            public TestEntity(Action<IEvent> applyChange)
                : this(new ConventionEventRouter(), applyChange)
            { }

            private TestEntity(ConventionEventRouter eventRouter, Action<IEvent> applyChange)
                : base(eventRouter, applyChange)
            {
                eventRouter.Register(this);
            }

            public int Id { get; private set; }

            public string Text { get; private set; }

            public void UpdateText(string newText)
            {
                if (String.IsNullOrWhiteSpace(newText))
                    throw DomainError.Named("entity-text-validation", "Entity text cannot be null, empty or whitespace");

                ApplyChange(new EntityTextUpdated(_testId, Id, newText));
            }

            #region EventHandlers

            private void When(EntityAdded e)
            {
                _testId = e.Id;
                Id = e.EntityId;
                Text = e.Text;
            }

            private void When(EntityTextUpdated e) { Text = e.UpdatedText; }

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

        [Serializable]
        [DataContract(Namespace = "TestContractNamespace")]
        public class EntityTextUpdated : IEvent<TestId>
        {
            public EntityTextUpdated(TestId aggregateId, int entityId, string updatedText)
            {
                Id = aggregateId;
                EntityId = entityId;
                UpdatedText = updatedText;
            }

            [DataMember(Order = 1)] public TestId Id { get; private set; }
            [DataMember(Order = 2)] public int EntityId { get; private set; }
            [DataMember(Order = 3)] public string UpdatedText { get; private set; }
        }
    }
}