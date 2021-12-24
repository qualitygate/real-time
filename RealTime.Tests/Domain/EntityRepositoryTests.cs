using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QualityGate.RealTime.Domain;
using QualityGate.RealTime.Queries;
using Raven.Client.Documents;
using Raven.TestDriver;

namespace QualityGate.RealTime.Tests.Domain
{
    [TestClass]
    public class EntityRepositoryTests : RavenTestDriver
    {
        private const string ConnectionId = "Connection: #1";
        private const string QueryName = "Query: #1";
        private const string TableName = "TestEntities";


        [TestInitialize]
        public void Initialize()
        {
            try
            {
                var testServerOptions = new TestServerOptions
                {
                    FrameworkVersion = "5.0.3+"
                };

                ConfigureServer(testServerOptions);
            }
            catch (InvalidOperationException)
            {
            }
        }


        [TestMethod]
        public void FindPageAsync_ReturnsRequestedPage()
        {
            // Given some entities in the database
            var store = GetDocumentStore();

            var entities = CreateEntities(store);

            // When requested results for a paginated query
            var repository = new EntityRepository(store);
            var query = new PaginatedQuery(ConnectionId, QueryName, TableName)
            {
                Conditions = new[] { new Condition(nameof(TestEntity.Age), OperatorBase.Equal, 10) },
                OrderBy = new OrderBy { Fields = new[] { nameof(TestEntity.Name) }, Ascending = false },
                Page = 1,
                Size = 2
            };
            var pageInfo = repository.FindPageAsync<TestEntity>(query).WaitForResult();

            // Then
            var expectedPage = new PageInfo<TestEntity>(
                5, 
                new[] { entities.Single(x => x.Name == "3"), entities.Single(x => x.Name == "2") }, 
                1, 
                2);
            pageInfo.AssertEqualByValues(expectedPage, nameof(PageInfo<object>.Items));

            Assert.AreEqual(2, pageInfo.Items.Length);
            Assert.AreEqual(expectedPage.Items.First(), pageInfo.Items.First());
            Assert.AreEqual(expectedPage.Items.Last(), pageInfo.Items.Last());
        }

        [TestMethod]
        public void FindAllAsync_ReturnsAllElementsMatchingQuery()
        {
            // Given some entities in the database
            var store = GetDocumentStore();
            var entities = CreateEntities(store);
            var repository = new EntityRepository(store);

            // When requested results for a paginated query
            var query = new Query(ConnectionId, QueryName, TableName)
            {
                Conditions = new[] { new Condition(nameof(TestEntity.Age), OperatorBase.Equal, 10) },
                OrderBy = new OrderBy { Fields = new[] { nameof(TestEntity.Name) }, Ascending = false }
            };
            var result = repository.FindAllAsync<TestEntity>(query).WaitForResult();

            // Then
            var expectedEntities = new[] { entities[7], entities[5], entities[2], entities[1], entities[0] };
            CollectionAssert.AreEquivalent(expectedEntities, result);
        }

        [TestMethod]
        public void FindAsync_GivenEntityId_ReturnsEntityWithGivenId()
        {
            // Given some entities that already exist in the database
            var store = GetDocumentStore();
            var entities = CreateEntities(store);
            var repository = new EntityRepository(store);

            // When asked for the entity with a certain Id
            var foundEntity = repository.FindAsync<TestEntity>(entities[2].Id).WaitForResult();

            // The correct entity must be returned
            foundEntity.AssertEqualByValues(entities[2]);
        }

        private static TestEntity[] CreateEntities(IDocumentStore store)
        {
            var targetEntity1 = new TestEntity { Name = "1", Age = 10 };
            var targetEntity2 = new TestEntity { Name = "2", Age = 10 };
            var targetEntity3 = new TestEntity { Name = "3", Age = 10 };
            var targetEntity4 = new TestEntity { Name = "4", Age = 11 };
            var targetEntity5 = new TestEntity { Name = "5", Age = 12 };
            var targetEntity6 = new TestEntity { Name = "6", Age = 10 };
            var targetEntity7 = new TestEntity { Name = "6", Age = 12 };
            var targetEntity8 = new TestEntity { Name = "8", Age = 10 };
            var targetEntity9 = new TestEntity { Name = "9", Age = 12 };
            var anotherEntity1 = new AnotherEntity { Age = 1 };
            var anotherEntity2 = new AnotherEntity { Age = 2 };
            var testEntities = new[]
            {
                targetEntity1,
                targetEntity2,
                targetEntity3,
                targetEntity4,
                targetEntity5,
                targetEntity6,
                targetEntity7,
                targetEntity8,
                targetEntity9
            };
            var anotherEntities = new[] { anotherEntity1, anotherEntity2 };

            using (var setupSession = store.OpenSession())
            {
                foreach (var testEntity in testEntities) setupSession.Store(testEntity);
                foreach (var testEntity in anotherEntities) setupSession.Store(testEntity);
                setupSession.Store(targetEntity1);
                setupSession.Store(targetEntity2);
                setupSession.Store(targetEntity3);
                setupSession.Store(targetEntity4);
                setupSession.Store(targetEntity5);
                setupSession.Store(targetEntity6);
                setupSession.Store(targetEntity7);
                setupSession.Store(targetEntity8);
                setupSession.Store(targetEntity9);
                setupSession.Store(anotherEntity1);
                setupSession.Store(anotherEntity2);
                setupSession.SaveChanges();
            }

            return testEntities;
        }
    }
}