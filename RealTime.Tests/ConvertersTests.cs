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
                    new ConditionDto("LastName", Operator.Matches, JoinOperator.And, true, Value: "*caster*"),
                    new ConditionDto("Name", Operator.NotEqual, JoinOperator.And, Value: "John", RightParenthesis: true),
                    new ConditionDto("Age", Operator.Equal, JoinOperator.Or, Value: 30),
                    new ConditionDto("Age", Operator.Equal, Value: 31)
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
            var expectedQuery = new PaginatedQuery(connectionId, "query#1", "entities")
            {
                Fields = new[] { "Name", "Age" },
                Conditions = new[]
                {
                    new Condition("LastName", Operator.Matches, "*caster*") {JoinUsing = JoinOperator.And, LeftParenthesis = true},
                    new Condition("Name", Operator.NotEqual, "John") {JoinUsing = JoinOperator.And, RightParenthesis = true},
                    new Condition("Age", Operator.Equal, 30) {JoinUsing = JoinOperator.Or},
                    new Condition("Age", Operator.Equal, 31)
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
                Assert.AreEqual(c1.LeftParenthesis, c2.LeftParenthesis);
                Assert.AreEqual(c1.RightParenthesis, c2.RightParenthesis);
            }

            Assert.AreEqual(expectedQuery.Conditions.Length, query.Conditions!.Length);
            for (var i = 0; i < expectedQuery.Conditions.Length; i++)
                AssertEqual(expectedQuery.Conditions[i], query.Conditions![i]);

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
                    new ConditionDto("LastName", Operator.Matches, JoinOperator.And.Operator, true, Value: "*caster*"),
                    new ConditionDto("Name", Operator.NotEqual, JoinOperator.And.Operator, Value: "John", RightParenthesis: true),
                    new ConditionDto("Age", Operator.Equal, JoinOperator.Or.Operator, Value: 30),
                    new ConditionDto("Age", Operator.Equal, Value: 31)
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
                    new Condition("LastName", Operator.Matches, "*caster*") {JoinUsing = JoinOperator.And, LeftParenthesis = true},
                    new Condition("Name", Operator.NotEqual, "John") {JoinUsing = JoinOperator.And, RightParenthesis = true},
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
                Assert.AreEqual(c1.LeftParenthesis, c2.LeftParenthesis);
                Assert.AreEqual(c1.RightParenthesis, c2.RightParenthesis);
            }

            Assert.AreEqual(4, query.Conditions!.Length);
            for (var i = 0; i < expectedQuery.Conditions.Length; i++)
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