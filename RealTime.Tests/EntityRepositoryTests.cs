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


        [TestMethod]
        public void Find_GivenQuery_ReturnsEntitiesSatisfyingIt()
        {
            // Given
            var query = new Query("c#1", "all", "entities");
            IEntity[] expectedEntities = { Stubs.NewEntity with { Id = Guid.NewGuid().ToString() } };

            var queryResult = Substitute.For<IAsyncRawDocumentQuery<IEntity>>();
            queryResult.ToArrayAsync().Returns(expectedEntities);
            _advancedOperations.AsyncRawQuery<IEntity>(query).Returns(queryResult);

            // When
            var result = _subject.Find<IEntity>(query).WaitForResult();

            // Then
            CollectionAssert.AreEquivalent(expectedEntities, result);
        }

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