using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QualityGate.RealTime.Changes;
using QualityGate.RealTime.Queries;

namespace QualityGate.RealTime.Tests.Queries
{
    [TestClass]
    public class QueryTests
    {
        private const string ConnectionId = "1";
        private const string TableName = "entities";

        private Query? _subject;


        [TestInitialize]
        public void Initialize()
        {
            _subject = new Query(ConnectionId, "query#1", TableName);
        }


        [TestMethod]
        public void MatchesChange_QueryWithConditionsMatchesChangedEntity_ReturnsTrue()
        {
            // Given
            var entity = Stubs.NewEntity;
            var change = new Change(entity, TableName, ChangeType.Upsert);
            _subject = _subject! with
            {
                Conditions = new[]
                {
                    new Condition(nameof(TestEntity.Id), Operator.Equal, entity.Id)
                        { JoinUsing = JoinOperator.And },
                    new Condition(nameof(TestEntity.Name), Operator.Equal, entity.Name)
                }
            };

            // When
            var result = _subject.MatchesChange(change);

            // Then
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MatchesChange_QueryWithConditionsDoesNotMatchChangedEntity_ReturnsTrue()
        {
            // Given
            var entity = Stubs.NewEntity;
            var change = new Change(entity, TableName, ChangeType.Upsert);
            _subject = _subject! with
            {
                Conditions = new[]
                {
                    new Condition(nameof(TestEntity.Id), Operator.Equal, -1)
                        { JoinUsing = JoinOperator.And },
                    new Condition(nameof(TestEntity.Name), Operator.Equal, entity.Name)
                }
            };

            // When
            var result = _subject.MatchesChange(change);

            // Then
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MatchesChange_QueryWithoutConditionsMatchesChangedEntity_ReturnsTrue()
        {
            // Given
            var entity = Stubs.NewEntity;
            var change = new Change(entity, TableName, ChangeType.Upsert);

            // When
            var result = _subject!.MatchesChange(change);

            // Then
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MatchesChange_QueryEmptyConditionsMatchesChangedEntity_ReturnsTrue()
        {
            // Given
            var entity = Stubs.NewEntity;
            var change = new Change(entity, TableName, ChangeType.Upsert);
            _subject = _subject! with { Conditions = Array.Empty<Condition>() };

            // When
            var result = _subject.MatchesChange(change);

            // Then
            Assert.IsTrue(result);
        }

        #region Implicit converter

        [TestMethod]
        public void ImplicitStringConverter_GivenFullyDefinedQuery_ReturnsProperStringRepresentation()
        {
            var query = new PaginatedQuery(ConnectionId, _subject!.Name, _subject.Table)
            {
                Fields = new[] { "Name", "Id" },
                Conditions = new[]
                {
                    new Condition("Name", Operator.Equal, "Senna") { JoinUsing = JoinOperator.And },
                    new Condition("Name", Operator.Matches, "*John*") { JoinUsing = JoinOperator.Or },
                    new Condition("Id", Operator.Equal, 1) { JoinUsing = JoinOperator.Or },
                    new Condition("Id", Operator.NotEqual, 2)
                },
                OrderBy = new OrderBy
                {
                    Ascending = false,
                    Fields = new[] { "Name", "Id" }
                },
                Size = 30,
                Page = 10
            };

            // When
            string queryString = query;

            // Then
            Assert.AreEqual(
                "from entities where Name = 'Senna' and search(Name, '*John*') or Id = 1 or Id <> 2 order by Name, Id desc select Name, Id",
                queryString);
        }

        #endregion
    }
}