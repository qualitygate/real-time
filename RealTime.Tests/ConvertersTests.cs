using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QualityGate.RealTime.Queries;
using QualityGate.RealTime.Utils;

namespace QualityGate.RealTime.Tests
{
    [TestClass]
    public class ConvertersTests
    {
        [TestMethod]
        public void ToQuery_GivenQueryDto_ConvertsItCorrectly()
        {
            var queryDto = new QueryDto
            {
                Fields = new[] { "Name", "Age" },
                Name = "query#1",
                Table = "entities",
                Conditions = new[]
                {
                    new ConditionDto("Name", "=", "John", JoinOperator.And.Operator),
                    new ConditionDto("Age", "=", 30)
                },
                OrderBy = new OrderBy
                {
                    Fields = new[] { "Name", "Age" },
                    Ascending = true
                }
            };
            const string connectionId = "connection#1";

            // When
            var query = queryDto.ToQuery(connectionId);

            // Then all fields of the query match (skip for now Conditions, Fields and OrderBy
            var expectedQuery = new PaginatedQuery(connectionId, "query#1", "entities", 10, 15)
            {
                Fields = new[] { "Name", "Age" },
                Conditions = new[]
                {
                    new Condition("Name", Operator.Equal, "John") {JoinUsing = JoinOperator.And},
                    new Condition("Age", Operator.Equal, 30)
                },
                OrderBy = queryDto.OrderBy
            };
            Assert.IsInstanceOfType(query, typeof(Query));
            query.AssertEqualByValues(expectedQuery, nameof(Query.Fields), nameof(Query.Conditions), nameof(OrderBy));

            // Assert the Fields
            CollectionAssert.AreEquivalent(expectedQuery.Fields, query.Fields);

            // Assert conditions
            void AssertEqual(Condition c1, Condition c2)
            {
                Assert.AreEqual(c1.Field, c2.Field);
                Assert.AreEqual(c1.Operator.GetType().FullName, c2.Operator.GetType().FullName);
                Assert.AreEqual(c1.Value, c2.Value);
                Assert.AreEqual(c1.JoinUsing, c2.JoinUsing);
            }

            Assert.AreEqual(2, query.Conditions!.Length);
            AssertEqual(expectedQuery.Conditions[0], query.Conditions![0]);
            AssertEqual(expectedQuery.Conditions[1], query.Conditions![1]);

            // Assert the order by
            CollectionAssert.AreEquivalent(expectedQuery.OrderBy!.Fields!.ToArray(), query.OrderBy!.Fields!.ToArray());
            Assert.AreEqual(expectedQuery.OrderBy.Ascending, query.OrderBy.Ascending);
        }

        [TestMethod]
        public void ToQuery_IfDefinedPageAndSize_ConvertsItCorrectlyToPaginatedQuery()
        {
            var queryDto = new QueryDto
            {
                Fields = new[] { "Name", "Age" },
                Name = "query#1",
                Table = "entities",
                Conditions = new[]
                {
                    new ConditionDto("LastName", Operator.Matches, "*caster*", JoinOperator.And.Operator),
                    new ConditionDto("Name", Operator.NotEqual, "John", JoinOperator.And.Operator),
                    new ConditionDto("Age", Operator.Equal, 30, JoinOperator.Or.Operator),
                    new ConditionDto("Age", Operator.Equal, 31)
                },
                OrderBy = new OrderBy
                {
                    Fields = new[] { "Name", "Age" },
                    Ascending = true
                },
                Page = 10,
                Size = 15
            };
            const string connectionId = "connection#1";

            // When
            var query = queryDto.ToQuery(connectionId);

            // Then all fields of the query match (skip for now Conditions, Fields and OrderBy
            var expectedQuery = new PaginatedQuery(connectionId, "query#1", "entities", 10, 15)
            {
                Fields = new[] { "Name", "Age" },
                Conditions = new[]
                {
                    new Condition("LastName", Operator.Matches, "*caster*") {JoinUsing = JoinOperator.And},
                    new Condition("Name", Operator.NotEqual, "John") {JoinUsing = JoinOperator.And},
                    new Condition("Age", Operator.Equal, 30) {JoinUsing = JoinOperator.Or},
                    new Condition("Age", Operator.Equal, 31)
                },
                OrderBy = queryDto.OrderBy
            };
            Assert.IsInstanceOfType(query, typeof(PaginatedQuery));
            query.AssertEqualByValues(expectedQuery, nameof(Query.Fields), nameof(Query.Conditions), nameof(OrderBy));

            // Assert the Fields
            CollectionAssert.AreEquivalent(expectedQuery.Fields, query.Fields);

            // Assert conditions
            void AssertEqual(Condition c1, Condition c2)
            {
                Assert.AreEqual(c1.Field, c2.Field);
                Assert.AreEqual(c1.Operator.GetType().FullName, c2.Operator.GetType().FullName);
                Assert.AreEqual(c1.Value, c2.Value);
                Assert.AreEqual(c1.JoinUsing, c2.JoinUsing);
            }

            Assert.AreEqual(4, query.Conditions!.Length);
            for (var i = 0; i < query.Conditions.Length; i++)
                AssertEqual(expectedQuery.Conditions[i], query.Conditions![i]);

            // Assert the order by
            CollectionAssert.AreEquivalent(expectedQuery.OrderBy!.Fields!.ToArray(), query.OrderBy!.Fields!.ToArray());
            Assert.AreEqual(expectedQuery.OrderBy.Ascending, query.OrderBy.Ascending);

            // Assert the Page and Size
            Assert.AreEqual(expectedQuery.Page, queryDto.Page);
            Assert.AreEqual(expectedQuery.Size, queryDto.Size);
        }
    }
}