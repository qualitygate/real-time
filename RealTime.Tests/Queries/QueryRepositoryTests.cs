using System.Collections;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using QualityGate.RealTime.Changes;
using QualityGate.RealTime.Domain;
using QualityGate.RealTime.Queries;

namespace QualityGate.RealTime.Tests.Queries
{
    [TestClass]
    public class QueryRepositoryTests
    {
        private const string ConnectionId = "1";
        private const string TableName = "entities";

        private ILogger<QueryRepository> _logger;

        private QueryRepository _subject;


        [TestInitialize]
        public void Initialize()
        {
            _logger = Substitute.For<ILogger<QueryRepository>>();

            _subject = new QueryRepository(_logger);
        }


        [TestMethod]
        public void AddQuery_GivenQuery_AddsIt()
        {
            var query = CreateQuery();

            // When commanded to add the query
            _subject.AddQuery(query);

            // Then
            CollectionAssert.AreEquivalent(new[] { query }, _subject.ToArray());
        }

        [TestMethod]
        public void ModifyQuery_ModifiesTheQuery()
        {
            // Given
            var query = CreateQuery();

            // When
            _subject.ModifyQuery(query with { Table = "AnotherTable" });

            // Then
            CollectionAssert.AreEquivalent(new[] { query with { Table = "AnotherTable" } }, _subject.ToArray());
        }

        [TestMethod]
        public void RemoveAllQueries_GivenConnectionId_RemovesQueriesAssociatedToGivenConnectionId()
        {
            var query = CreateQuery();
            _subject.AddQuery(query);
            _subject.AddQuery(query);

            // When commanded to add the query
            _subject.RemoveQuery(query);

            // Then
            Assert.AreEqual(0, _subject.ToArray().Length);
        }

        [TestMethod]
        public void RemoveQuery_GivenQuery_RemovesIt()
        {
            const string targetConnectionId = "2";
            var query1 = CreateQuery();
            var query2 = CreateQuery(targetConnectionId);
            _subject.AddQuery(query1);
            _subject.AddQuery(query2);

            // When commanded to add the query
            _subject.RemoveAllQueries(targetConnectionId);

            // Then
            CollectionAssert.AreEquivalent(new[] { query1 }, _subject.ToArray());
        }

        [TestMethod]
        public void SelectMatching_GivenChangeMatchingAQueryTableName_SelectsMatchingQueries()
        {
            var entity = Stubs.NewEntity;

            const string otherEntitiesTable = "otherEntities";
            var change = new Change(entity, otherEntitiesTable, ChangeType.Upsert);

            var query1 = CreateQuery();
            var query2 = new Query(ConnectionId, "query#2", otherEntitiesTable);
            _subject.AddQuery(query1);
            _subject.AddQuery(query2);

            // When commanded to add the query
            var queries = _subject.SelectMatching(change);

            // Then
            CollectionAssert.AreEquivalent(new[] { query2 }, queries);
        }

        [TestMethod]
        public void SelectMatchingTable_GivenChangeMatchingAQueryTableName_SelectsMatchingQueries()
        {
            var entity = Stubs.NewEntity;

            const string otherEntitiesTable = "otherEntities";
            var change = new Change(entity, TableName, ChangeType.Upsert);

            var query1 = CreateQuery();
            var query2 = new Query(ConnectionId, "query#2", otherEntitiesTable);
            var query3 = query1 with
            {
                Name = "query#3",
                Conditions = new[] { new Condition(nameof(IEntity.Id), OperatorBase.Eq, 2) }
            };
            _subject.AddQuery(query1);
            _subject.AddQuery(query2);
            _subject.AddQuery(query3);

            // When commanded to add the query
            Query[] queries = _subject.SelectMatchingTable(change);

            // Then
            CollectionAssert.AreEquivalent(new[] { query1, query3 }, queries);
        }

        [TestMethod]
        public void UnTypedGetEnumerator_ReturnsProperEnumerator()
        {
            var query = CreateQuery();

            // When commanded to add the query
            foreach (var element in (IEnumerable)_subject) Assert.AreSame(query, element);
        }


        private static Query CreateQuery(string connectionId = ConnectionId) =>
            new(connectionId, "query#1", TableName)
            {
                Conditions = new[] { new Condition(nameof(IEntity.Id), OperatorBase.Eq, 1) }
            };
    }
}