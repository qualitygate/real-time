using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using QualityGate.RealTime.Changes;
using QualityGate.RealTime.Domain;
using QualityGate.RealTime.Notifications;
using QualityGate.RealTime.Queries;
using Raven.Client.Documents.Changes;
using Raven.Client.Documents.Session;

namespace QualityGate.RealTime.Tests.Notification
{
    [TestClass]
    public class ChangeNotifierTests
    {
        private static readonly string ClientConnectionId = Guid.NewGuid().ToString();

        private IClientPool _clientPool;

        private IEntityRepository _entityRepository;
        private ILogger<ChangeNotifier> _logger;
        private IQueryRepository _queryRepository;

        private ChangeNotifier _subject;


        [TestInitialize]
        public void Initialize()
        {
            _clientPool = Substitute.For<IClientPool>();

            _entityRepository = Substitute.For<IEntityRepository>();
            _logger = Substitute.For<ILogger<ChangeNotifier>>();
            _queryRepository = Substitute.For<IQueryRepository>();

            _subject = new ChangeNotifier(_clientPool, _queryRepository, _logger, _entityRepository);
        }


        [TestMethod]
        public void NotifyFirstTime_GivenCorrectQuery_NotifiesTheResultOfExecutingIt()
        {
            // Given some entities and a registered query
            var (entities, query) = PrepareScenario();

            // When a query that would yield such entities gets registered.
            _subject.NotifyFirstTime(query).WaitFor();

            // Then, a notification to the listening clients (the ones that registered the query) should arrive,
            // containing one change description for each entity satisfying the query
            _clientPool
                .Received()
                .InvokeMethodAsync(
                    ClientMethods.EntityChanged,
                    ClientConnectionId,
                    query.Name,
                    Arg.Is<ExternalChange[]>(changes =>
                        changes[0] == new ExternalChange(entities.First(), ChangeType.Upsert) &&
                        changes[1] == new ExternalChange(entities.Last(), ChangeType.Upsert)));
        }

        #region Notify

        [TestMethod]
        public void Notify_GivenHiloCollectionChanges_IgnoresThem()
        {
            // Given a notification of a HILO collection in RavenDB
            var documentChange = new DocumentChange { CollectionName = "@hilo" };

            // When invoked the notification method
            _subject.Notify(documentChange).WaitFor();

            // No notification is sent anywhere
            _clientPool
                .Received(0)
                .InvokeMethodAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<ExternalChange[]>());
        }

        [TestMethod]
        public void Notify_GivenEntityChange_NotifiesClientsAboutThatChange()
        {
            // Given some entities and a registered query
            var (entities, (_, name, table)) = PrepareScenario();
            var changedEntity = entities.First();
            var documentChange = new DocumentChange
            {
                Id = changedEntity.Id,
                Type = DocumentChangeTypes.Put,
                CollectionName = table
            };

            // When a change that would impact any of the entities matching the query's criteria
            _subject.Notify(documentChange).WaitFor();

            // Then, a notification to the listening clients (the ones that registered the query) should arrive,
            // containing one change description for each entity satisfying the query
            _clientPool
                .Received()
                .InvokeMethodAsync(
                    ClientMethods.EntityChanged,
                    ClientConnectionId,
                    name,
                    Arg.Is<ExternalChange[]>(changes =>
                        changes[0] == new ExternalChange(entities.First(), ChangeType.Upsert)));
        }

        [TestMethod]
        public void
            Notify_GivenEntityChangeAndEntityNoLongerBelongsToAQueryMatchingEntityTable_NotifiesSuchQueriesAsWellPassingADeletionChange()
        {
            // Given some entities and a registered query
            (IEntity[] entities, Query query) = PrepareScenario();

            query = query with { Conditions = new[] { new Condition(nameof(IEntity.Id), OperatorBase.Eq, 300) } };
            var changedEntity = entities.First();
            var documentChange = new DocumentChange
            {
                Id = changedEntity.Id,
                Type = DocumentChangeTypes.Put,
                CollectionName = query.Table
            };

            // The repository does not anymore recognize the query as a directly impacted one
            _queryRepository
                .SelectMatching(Arg.Is<Change>(
                    c => c.Entity.Id == entities.First().Id &&
                         c.Table == query.Table &&
                         c.Type == ChangeType.Upsert))
                .Returns(Array.Empty<Query>());

            // But recognizes the it's a query matching entities of the same type as the changed one
            _queryRepository
                .SelectMatchingTable(Arg.Is<Change>(
                    c => c.Entity.Id == entities.First().Id &&
                         c.Table == query.Table &&
                         c.Type == ChangeType.Upsert))
                .Returns(new[] { query });

            // When a change that would stop impacting any of the entities matching the query's criteria
            _subject.Notify(documentChange).WaitFor();

            // The no notification is sent for the entity's update
            _clientPool
                .Received(0)
                .InvokeMethodAsync(
                    ClientMethods.EntityChanged,
                    ClientConnectionId,
                    query.Name,
                    Arg.Is<ExternalChange[]>(changes =>
                        changes[0] == new ExternalChange(entities.First(), ChangeType.Upsert)));

            // But, a notification to the listening clients (the ones that registered the query) should arrive,
            // "simulating" the changed entity was deleted, so that listening clients can remove the change entity from
            // their results, as it no longer satisfies the query's criteria
            _clientPool
                .Received()
                .InvokeMethodAsync(
                    ClientMethods.EntityChanged,
                    ClientConnectionId,
                    query.Name,
                    Arg.Is<ExternalChange[]>(changes =>
                        changes[0] == new ExternalChange(entities.First(), ChangeType.Delete)));
        }

        [TestMethod]
        public void Notify_GivenEntityDeletion_NotifiesClientsAboutThatDeletion()
        {
            // Given some entities and a registered query, one of the entities is deleted
            var (entities, (_, name, table)) = PrepareScenario(ChangeType.Delete);
            var deletedEntity = entities.First();
            var documentChange = new DocumentChange
            {
                Id = deletedEntity.Id,
                Type = DocumentChangeTypes.Delete,
                CollectionName = table
            };

            // When a query that would yield such entities gets registered.
            _subject.Notify(documentChange).WaitFor();

            // Then, a notification to the listening clients (the ones that registered the query) should arrive,
            // containing one change description for each entity satisfying the query
            _clientPool
                .Received()
                .InvokeMethodAsync(
                    ClientMethods.EntityChanged,
                    ClientConnectionId,
                    name,
                    Arg.Is<ExternalChange[]>(changes =>
                        changes[0] == new ExternalChange(new DeletedEntity(deletedEntity.Id), ChangeType.Delete)
                    )
                );
        }

        #endregion

        private TestData PrepareScenario(ChangeType entityChange = ChangeType.Upsert)
        {
            IEntity[] entities = { Stubs.NewEntity, Stubs.NewEntity };

            var query = new Query(ClientConnectionId, "query#1", "entities");
            var matchedQuery = query with { };

            _queryRepository
                .SelectMatching(Arg.Is<Change>(
                    c => c.Entity.Id == entities.First().Id &&
                         c.Table == query.Table &&
                         c.Type == entityChange))
                .Returns(new[] { matchedQuery });
            _queryRepository
                .SelectMatching(Arg.Is<Change>(
                    c => c.Entity.Id == entities.Last().Id &&
                         c.Table == query.Table &&
                         c.Type == entityChange))
                .Returns(new[] { matchedQuery });

            var asyncDocumentQuery = Substitute.For<IAsyncRawDocumentQuery<IEntity>>();
            asyncDocumentQuery.ToArrayAsync().Returns(entities);

            _entityRepository.Find<IEntity>(query).Returns(entities);
            _entityRepository.Find<IEntity>(entities.First().Id!).Returns(entities.First());
            _entityRepository.Find<IEntity>(entities.Last().Id!).Returns(entities.Last());

            _clientPool
                .InvokeMethodAsync(ClientConnectionId, ClientMethods.EntityChanged, query.Name,
                    Arg.Any<ExternalChange[]>())
                .Returns(Task.CompletedTask);

            return new TestData(entities, query);
        }

        private record TestData(IEntity[] Entities, Query Query);
    }
}