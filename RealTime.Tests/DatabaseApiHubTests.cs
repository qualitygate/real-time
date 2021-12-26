using System;
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

        private IChangeNotifier? _notifier;
        private IQueryRepository? _repository;

        private DatabaseApiHub? _subject;


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
            _subject!.AddQuery(queryDto).WaitFor();

            // Then
            _repository!
                .Received()
                .AddQuery(Arg.Is<Query>(
                    q => q.ConnectionId == _subject.Context.ConnectionId &&
                         q.Name == QueryName &&
                         q.Table == TableName &&
                         q.Fields!.Length == 2 &&
                         q.Conditions![0].Field == "Name" &&
                         q.Conditions[0].Operator.Symbol == Operator.Equal.Symbol &&
                         q.Conditions[0].Value as string == "John" &&
                         q.Conditions[0].JoinUsing == JoinOperator.And &&
                         q.Conditions[1].Field == "Age" &&
                         q.Conditions[1].Operator.Symbol == Operator.Equal.Symbol &&
                         (int)q.Conditions[1].Value! == 30 &&
                         q.Conditions[1].JoinUsing == null));

            _notifier!
                .Received()
                .NotifyFullResults(Arg.Is<Query>(
                    q => q.ConnectionId == _subject.Context.ConnectionId &&
                         q.Name == QueryName &&
                         q.Table == TableName &&
                         q.Fields!.Length == 2 &&
                         q.Conditions![0].Field == "Name" &&
                         q.Conditions[0].Operator.Symbol == Operator.Equal.Symbol &&
                         q.Conditions[0].Value as string == "John" &&
                         q.Conditions[0].JoinUsing == JoinOperator.And &&
                         q.Conditions[1].Field == "Age" &&
                         q.Conditions[1].Operator.Symbol == Operator.Equal.Symbol &&
                         (int)q.Conditions[1].Value! == 30 &&
                         q.Conditions[1].JoinUsing == null));
        }

        [TestMethod]
        public void Modify_GivenQueryDTO_ExecutesItsRegistration()
        {
            // Given
            var queryDto = CreateQueryDto();

            // When
            _subject!.ModifyQuery(queryDto).WaitFor();

            // Then
            _repository!
                .Received()
                .ModifyQuery(Arg.Is<Query>(
                    q => q.ConnectionId == _subject.Context.ConnectionId &&
                         q.Name == QueryName &&
                         q.Table == TableName &&
                         q.Fields!.Length == 2 &&
                         q.Conditions![0].Field == "Name" &&
                         q.Conditions[0].Operator.Symbol == Operator.Equal.Symbol &&
                         q.Conditions[0].Value as string == "John" &&
                         q.Conditions[0].JoinUsing == JoinOperator.And &&
                         q.Conditions[1].Field == "Age" &&
                         q.Conditions[1].Operator.Symbol == Operator.Equal.Symbol &&
                         (int)q.Conditions[1].Value! == 30 &&
                         q.Conditions[1].JoinUsing == null));

            _notifier!
                .Received()
                .NotifyFullResults(Arg.Is<Query>(
                    q => q.ConnectionId == _subject.Context.ConnectionId &&
                         q.Name == QueryName &&
                         q.Table == TableName &&
                         q.Fields!.Length == 2 &&
                         q.Conditions![0].Field == "Name" &&
                         q.Conditions[0].Operator.Symbol == Operator.Equal.Symbol &&
                         q.Conditions[0].Value as string == "John" &&
                         q.Conditions[0].JoinUsing == JoinOperator.And &&
                         q.Conditions[1].Field == "Age" &&
                         q.Conditions[1].Operator.Symbol == Operator.Equal.Symbol &&
                         (int)q.Conditions[1].Value! == 30 &&
                         q.Conditions[1].JoinUsing == null));
        }

        [TestMethod]
        public void OnDisconnectedAsync_RemovesAllQueriesAssociatedToHubConnection()
        {
            // Given
            var exception = new ApplicationException();

            // When
            _subject!.OnDisconnectedAsync(exception).WaitFor();

            // Then
            _repository!.RemoveAllQueries(ConnectionId);
        }

        [TestMethod]
        public void RemoveQuery_GivenQueryDTO_RemovesQueryMatchingIt()
        {
            // Given
            var queryDto = CreateQueryDto();

            // When
            _subject!.RemoveQuery(queryDto).WaitFor();

            // Then
            _repository!
                .Received()
                .RemoveQuery(Arg.Is<Query>(
                    q => q.ConnectionId == _subject.Context.ConnectionId &&
                         q.Name == QueryName &&
                         q.Table == TableName &&
                         q.Fields!.Length == 2 &&
                         q.Conditions![0].Field == "Name" &&
                         q.Conditions[0].Operator.Symbol == Operator.Equal.Symbol &&
                         q.Conditions[0].Value as string == "John" &&
                         q.Conditions[0].JoinUsing == JoinOperator.And &&
                         q.Conditions[1].Field == "Age" &&
                         q.Conditions[1].Operator.Symbol == Operator.Equal.Symbol &&
                         (int)q.Conditions[1].Value! == 30 &&
                         q.Conditions[1].JoinUsing == null));
        }

        private static QueryDto CreateQueryDto() => new()
        {
            Name = QueryName,
            Table = TableName,
            Conditions = new ConditionDto[]
            {
                new("Name", Operator.Equal.Symbol, JoinOperator.And.Operator, Value: "John"),
                new("Age", Operator.Equal.Symbol, Value: 30)
            },
            Fields = new[] { "Name", "Age" },
            OrderBy = new OrderBy { Fields = new[] { "Name", "Age" }, Ascending = true }
        };
    }
}