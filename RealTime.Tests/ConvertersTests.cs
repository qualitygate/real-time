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
                    new ConditionDto("Name", "=", "John", ContinuationOperator.And.Operator),
                    new ConditionDto("Age", "=", 30)
                },
                OrderBy = new OrderBy
                {
                    Fields = new[] { "Name", "Age" },
                    Ascending = true
                },
                Take = 10,
                Skip = 15
            };
            const string connectionId = "connection#1";

            // When
            var query = queryDto.ToQuery(connectionId);

            // Then all fields of the query match (skip for now Conditions, Fields and OrderBy
            var expectedQuery = new Query(connectionId, "query#1", "entities")
            {
                Fields = new[] { "Name", "Age" },
                Conditions = new[]
                {
                    new Condition("Name", OperatorBase.Eq, "John") { ContinueWith = ContinuationOperator.And },
                    new Condition("Age", OperatorBase.Eq, 30)
                },
                OrderBy = queryDto.OrderBy,
                Skip = 15,
                Take = 10
            };
            query.AssertEqualByValues(expectedQuery, nameof(Query.Fields), nameof(Query.Conditions), nameof(OrderBy));

            // Assert the Fields
            CollectionAssert.AreEquivalent(expectedQuery.Fields, query.Fields);

            // Assert conditions
            void AssertEqual(Condition c1, Condition c2)
            {
                Assert.AreEqual(c1.Field, c2.Field);
                Assert.AreEqual(c1.Operator.GetType().FullName, c2.Operator.GetType().FullName);
                Assert.AreEqual(c1.Value, c2.Value);
                Assert.AreEqual(c1.ContinueWith, c2.ContinueWith);
            }

            Assert.AreEqual(2, query.Conditions!.Length);
            AssertEqual(expectedQuery.Conditions[0], query.Conditions![0]);
            AssertEqual(expectedQuery.Conditions[1], query.Conditions![1]);

            // Assert the order by
            CollectionAssert.AreEquivalent(expectedQuery.OrderBy!.Fields!.ToArray(), query.OrderBy!.Fields!.ToArray());
            Assert.AreEqual(expectedQuery.OrderBy.Ascending, query.OrderBy.Ascending);
        }
    }
}