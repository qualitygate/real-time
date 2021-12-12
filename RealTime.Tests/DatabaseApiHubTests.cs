using System;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using QualityGate.RealTime.Notifications;
using QualityGate.RealTime.Queries;

namespace QualityGate.RealTime.Tests
{
    [TestClass]
    public class DatabaseApiHubTests
    {
        private const string ConnectionId = "connection#1";
        private const string TableName = "entities";
        private const string QueryName = "all";

        private IChangeNotifier _notifier;
        private IQueryRepository _repository;

        private DatabaseApiHub _subject;


        [TestInitialize]
        public void Initialize()
        {
            var logger = Substitute.For<ILogger<DatabaseApiHub>>();
            _repository = Substitute.For<IQueryRepository>();
            _notifier = Substitute.For<IChangeNotifier>();

            var context = Substitute.For<HubCallerContext>();
            context.ConnectionId.Returns(ConnectionId);

            _subject = new DatabaseApiHub(logger, _repository, _notifier) { Context = context };
        }


        [TestMethod]
        public void AddQuery_GivenQueryDTO_ExecutesItsRegistration()
        {
            // Given
            var queryDto = CreateQueryDto();

            // When
            _subject.AddQuery(queryDto).WaitFor();

            // Then
            _repository
                .Received()
                .AddQuery(Arg.Is<Query>(
                    q => q.ConnectionId == _subject.Context.ConnectionId &&
                         q.Name == QueryName &&
                         q.Table == TableName &&
                         q.Fields.Length == 1 &&
                         q.Fields.Contains("*")));
            _notifier
                .Received()
                .NotifyFirstTime(Arg.Is<Query>(
                    q => q.ConnectionId == _subject.Context.ConnectionId &&
                         q.Name == QueryName &&
                         q.Table == TableName &&
                         q.Fields.Length == 1 &&
                         q.Fields.Contains("*")));
        }

        [TestMethod]
        public void OnDisconnectedAsync_RemovesAllQueriesAssociatedToHubConnection()
        {
            // Given
            var exception = new ApplicationException();

            // When
            _subject.OnDisconnectedAsync(exception).WaitFor();

            // Then
            _repository.RemoveAllQueries(ConnectionId);
        }

        [TestMethod]
        public void RemoveQuery_GivenQueryDTO_RemovesQueryMatchingIt()
        {
            // Given
            var queryDto = CreateQueryDto();

            // When
            _subject.RemoveQuery(queryDto).WaitFor();

            // Then
            _repository
                .Received()
                .RemoveQuery(Arg.Is<Query>(
                    q => q.ConnectionId == _subject.Context.ConnectionId &&
                         q.Name == QueryName &&
                         q.Table == TableName &&
                         q.Fields.Length == 1 &&
                         q.Fields.Contains("*")));
        }

        private static QueryDto CreateQueryDto() => new() { Name = QueryName, Table = TableName };
    }
}