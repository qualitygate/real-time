using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using QualityGate.RealTime.Domain;
using QualityGate.RealTime.Queries;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace QualityGate.RealTime.Tests
{
    [TestClass]
    public class EntityRepositoryTests
    {
        private IAsyncAdvancedSessionOperations _advancedOperations;
        private IDocumentStore _documentStore;
        private IAsyncDocumentSession _documentSession;

        private EntityRepository _subject;


        [TestInitialize]
        public void Initialize()
        {
            _advancedOperations = Substitute.For<IAsyncAdvancedSessionOperations>();
            _documentSession = Substitute.For<IAsyncDocumentSession>();
            _documentStore = Substitute.For<IDocumentStore>();

            _documentStore.OpenAsyncSession().Returns(_documentSession);
            _documentSession.Advanced.Returns(_advancedOperations);

            _subject = new EntityRepository(_documentStore);
        }


        #region Find all

        [TestMethod]
        public void Find_GivenQuery_ReturnsEntitiesSatisfyingIt()
        {
            // Given
            var query = new Query("c#1", "all", "entities");
            IEntity[] expectedEntities = { Stubs.NewEntity with { Id = Guid.NewGuid().ToString() } };

            var queryResult = Substitute.For<IAsyncRawDocumentQuery<IEntity>>();
            queryResult.Skip(0).Returns(queryResult);
            queryResult.Take(int.MaxValue).Returns(queryResult);
            queryResult.ToArrayAsync().Returns(expectedEntities);
            _advancedOperations.AsyncRawQuery<IEntity>(query).Returns(queryResult);

            // When
            var result = _subject.Find<IEntity>(query).WaitForResult();

            // Then
            CollectionAssert.AreEquivalent(expectedEntities, result);
        }

        [TestMethod]
        public void Find_GivenQueryWithSlicing_ReturnsSlicedQueryResult()
        {
            // Given
            var query = new Query("c#1", "all", "entities") { Skip = 2, Take = 3 };
            IEntity[] expectedEntities = { Stubs.NewEntity with { Id = Guid.NewGuid().ToString() } };

            var queryResult = Substitute.For<IAsyncRawDocumentQuery<IEntity>>();

            queryResult.Skip(2).Returns(queryResult);
            queryResult.Take(3).Returns(queryResult);
            queryResult.ToArrayAsync().Returns(expectedEntities);

            _advancedOperations.AsyncRawQuery<IEntity>(query).Returns(queryResult);

            // When
            var result = _subject.Find<IEntity>(query).WaitForResult();

            // Then
            CollectionAssert.AreEquivalent(expectedEntities, result);
            queryResult.Received().Skip(2);
            queryResult.Received().Take(3);
        }

        #endregion

        #region Find by id

        [TestMethod]
        public void Find_GivenIdAndEntityWasNotLoadedYet_ReturnsFoundEntity()
        {
            // Given
            IEntity entity = Stubs.NewEntity;

            _documentSession.LoadAsync<IEntity>(entity.Id).Returns(entity);

            // When
            var result = _subject.Find<IEntity>(entity.Id!).WaitForResult();

            // Then
            Assert.AreSame(entity, result);
        }

        #endregion
    }
}