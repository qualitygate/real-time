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

        private IClientPool? _clientPool;

        private IEntityRepository? _entityRepository;
        private ILogger<ChangeNotifier>? _logger;
        private IQueryRepository? _queryRepository;

        private ChangeNotifier? _subject;


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
        public void NotifyFullResults_GivenNormalQuery_NotifiesTheResultOfExecutingIt()
        {
            // Given some entities and a registered query
            var (entities, query) = PrepareScenario();

            // When a query that would yield such entities gets registered.
            _subject!.NotifyFullResults(query).WaitFor();

            // Then, a notification to the listening clients (the ones that registered the query) should arrive,
            // containing one change description for each entity satisfying the query
            _clientPool!
                .Received()
                .InvokeMethodAsync(
                    ClientMethods.EntityChanged,
                    ClientConnectionId,
                    query.Name,
                    Arg.Is<object[]>(changes =>
                        ((ExternalChange)changes[0]).Entity == entities.First() &&
                        ((ExternalChange)changes[0]).Type == ChangeType.Upsert &&
                        ((ExternalChange)changes[1]).Entity == entities.Last() &&
                        ((ExternalChange)changes[1]).Type == ChangeType.Upsert));
        }

        [TestMethod]
        public void NotifyFullResults_GivenPaginatedQuery_NotifiesTheResultOfExecutingIt()
        {
            // Given some entities and a registered paginated query
            var query = new PaginatedQuery(ClientConnectionId, "query#1", "entities", 1, 3);
            var (entities, _) = PrepareScenario(query: query);

            // When a query that would yield such entities gets registered.
            _subject!.NotifyFullResults(query).WaitFor();

            // Then, a notification to the listening clients (the ones that registered the query) should arrive,
            // containing one change description for each entity satisfying the query
            _clientPool!
                .Received()
                .InvokeMethodAsync(
                    ClientMethods.PageChanged,
                    ClientConnectionId,
                    query.Name,
                    Arg.Is<object>(p => ((PageInfo<object>)p).Total == entities.Length &&
                                        ((PageInfo<object>)p).Items[0] == entities[0] &&
                                        ((PageInfo<object>)p).Items[1] == entities[1] &&
                                        ((PageInfo<object>)p).Page == query.Page &&
                                        ((PageInfo<object>)p).Size == query.Size));
        }

        #region NotifyEntityChanged

        [TestMethod]
        public void NotifyEntityChanged_GivenHiloCollectionChanges_IgnoresThem()
        {
            // Given a notification of a HILO collection in RavenDB
            var documentChange = new DocumentChange { CollectionName = "@hilo" };

            // When invoked the notification method
            _subject!.NotifyEntityChanged(documentChange).WaitFor();

            // No notification is sent anywhere
            _clientPool!
                .Received(0)
                .InvokeMethodAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<ExternalChange[]>());
        }

        [TestMethod]
        public void NotifyEntityChanged_GivenEntityChangeAndMatchesAPaginatedQuery_NotifyClientsThatWholePaginatedQueryChanged()
        {
            // Given some entities and a registered query
            var query = new PaginatedQuery(ClientConnectionId, "query#1", "entities", 1, 3);
            var (entities, (_, name, table)) = PrepareScenario(query: query);
            var changedEntity = entities.First();
            var documentChange = new DocumentChange
            {
                Id = changedEntity.Id,
                Type = DocumentChangeTypes.Put,
                CollectionName = table
            };

            // When a change that would impact any of the entities matching the query's criteria
            _subject!.NotifyEntityChanged(documentChange).WaitFor();

            // Then, a notification to the listening clients (the ones that registered the query) should arrive,
            // containing one change description for each entity satisfying the query
            _clientPool!
                .Received()
                .InvokeMethodAsync(
                    ClientMethods.PageChanged,
                    ClientConnectionId,
                    name,
                    Arg.Is<object>(p => ((PageInfo<object>)p).Total == entities.Length &&
                                        ((PageInfo<object>)p).Items[0] == entities[0] &&
                                        ((PageInfo<object>)p).Items[1] == entities[1] &&
                                        ((PageInfo<object>)p).Page == query.Page &&
                                        ((PageInfo<object>)p).Size == query.Size));
        }

        [TestMethod]
        public void NotifyEntityChanged_GivenPaginatedQuery_NotifiesTheResultOfExecutingIt()
        {
            // Given some entities and a registered paginated query
            var query = new PaginatedQuery(ClientConnectionId, "query#1", "entities", 1, 3);
            var (entities, _) = PrepareScenario(query: query);

            // When a query that would yield such entities gets registered.
            _subject!.NotifyFullResults(query).WaitFor();

            // Then, a notification to the listening clients (the ones that registered the query) should arrive,
            // containing one change description for each entity satisfying the query
            _clientPool!
                .Received()
                .InvokeMethodAsync(
                    ClientMethods.PageChanged,
                    ClientConnectionId,
                    query.Name,
                    Arg.Is<object>(p => ((PageInfo<object>)p).Total == entities.Length &&
                                        ((PageInfo<object>)p).Items[0] == entities[0] &&
                                        ((PageInfo<object>)p).Items[1] == entities[1] &&
                                        ((PageInfo<object>)p).Page == query.Page &&
                                        ((PageInfo<object>)p).Size == query.Size));
        }

        [TestMethod]
        public void
            NotifyEntityChanged_GivenEntityChangeAndEntityNoLongerBelongsToAQueryMatchingEntityTable_NotifiesSuchQueriesAsWellPassingADeletionChange()
        {
            // Given some entities and a registered query
            var (entities, query) = PrepareScenario();

            query = query with { Conditions = new[] { new Condition(nameof(IEntity.Id), OperatorBase.Equal, 300) } };
            var changedEntity = entities.First();
            var documentChange = new DocumentChange
            {
                Id = changedEntity.Id,
                Type = DocumentChangeTypes.Put,
                CollectionName = query.Table
            };

            // The repository does not anymore recognize the query as a directly impacted one
            _queryRepository!
                .SelectMatching(Arg.Is<Change>(
                    c => ((IEntity)c.Entity).Id == entities.First().Id &&
                         c.Table == query.Table &&
                         c.Type == ChangeType.Upsert))
                .Returns(Array.Empty<Query>());

            // But recognizes the it's a query matching entities of the same type as the changed one
            _queryRepository
                .SelectMatchingTable(Arg.Is<Change>(
                    c => ((IEntity)c.Entity).Id == entities.First().Id &&
                         c.Table == query.Table &&
                         c.Type == ChangeType.Upsert))
                .Returns(new[] { query });

            // When a change that would stop impacting any of the entities matching the query's criteria
            _subject!.NotifyEntityChanged(documentChange).WaitFor();

            // The no notification is sent for the entity's update
            _clientPool!
                .Received(0)
                .InvokeMethodAsync(
                    ClientMethods.EntityChanged,
                    ClientConnectionId,
                    query.Name,
                    Arg.Is<object[]>(changes =>
                        (ExternalChange)changes[0] == new ExternalChange(entities.First(), ChangeType.Upsert)));

            // But, a notification to the listening clients (the ones that registered the query) should arrive,
            // "simulating" the changed entity was deleted, so that listening clients can remove the change entity from
            // their results, as it no longer satisfies the query's criteria
            _clientPool!
                .Received()
                .InvokeMethodAsync(
                    ClientMethods.EntityChanged,
                    ClientConnectionId,
                    query.Name,
                    Arg.Is<object[]>(changes =>
                        (ExternalChange)changes[0] == new ExternalChange(entities.First(), ChangeType.Delete)));
        }

        [TestMethod]
        public void NotifyEntityChanged_GivenEntityDeletion_NotifiesClientsAboutThatDeletion()
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
            _subject!.NotifyEntityChanged(documentChange).WaitFor();

            // Then, a notification to the listening clients (the ones that registered the query) should arrive,
            // containing one change description for each entity satisfying the query
            _clientPool!
                .Received()
                .InvokeMethodAsync(
                    ClientMethods.EntityChanged,
                    ClientConnectionId,
                    name,
                    Arg.Is<object[]>(changes =>
                        (ExternalChange)changes[0] == new ExternalChange(new DeletedEntity(deletedEntity.Id!), ChangeType.Delete)
                    )
                );
        }

        #endregion

        private TestData PrepareScenario(ChangeType entityChange = ChangeType.Upsert, Query? query = null)
        {
            object[] entities = { Stubs.NewEntity, Stubs.NewEntity };

            query ??= new Query(ClientConnectionId, "query#1", "entities");
            var matchedQuery = query with { };

            _queryRepository!
                .SelectMatching(Arg.Is<Change>(
                    c => ((IEntity)c.Entity).Id == ((IEntity)entities.First()).Id &&
                         c.Table == query.Table &&
                         c.Type == entityChange))
                .Returns(new[] { matchedQuery });
            _queryRepository
                .SelectMatching(Arg.Is<Change>(
                    c => ((IEntity)c.Entity).Id == ((IEntity)entities.Last()).Id &&
                         c.Table == query.Table &&
                         c.Type == entityChange))
                .Returns(new[] { matchedQuery });

            var asyncDocumentQuery = Substitute.For<IAsyncRawDocumentQuery<object>>();
            asyncDocumentQuery.ToArrayAsync().Returns(entities);

            if (query is PaginatedQuery paginatedQuery)
            {
                _entityRepository!
                    .FindPageAsync<object>(paginatedQuery)
                    .Returns(new PageInfo<object>(
                        entities.Length,
                        entities,
                        paginatedQuery.Page,
                        paginatedQuery.Size));
            }
            else
            {
                _entityRepository!.FindAllAsync<object>(query).Returns(entities);
            }
            _entityRepository.FindAsync<object>(((IEntity)entities.First()).Id!).Returns(entities.First());
            _entityRepository.FindAsync<object>(((IEntity)entities.Last()).Id!).Returns(entities.Last());

            _clientPool!
                .InvokeMethodAsync(ClientConnectionId, ClientMethods.EntityChanged, query.Name,
                    Arg.Any<ExternalChange[]>())
                .Returns(Task.CompletedTask);

            return new TestData(entities.Cast<IEntity>().ToArray(), query);
        }

        private record TestData(IEntity[] Entities, Query Query);
    }
}