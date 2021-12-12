using System;
using System.Threading;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using QualityGate.RealTime.Changes;
using QualityGate.RealTime.Domain;
using QualityGate.RealTime.Notifications;

namespace QualityGate.RealTime.Tests.Notification
{
    [TestClass]
    public class ClientPoolTests
    {
        private static readonly string ConnectionId = Guid.NewGuid().ToString();

        private IClientProxy _client;
        private IHubContext<DatabaseApiHub> _hubContext;
        private IHubClients _hubClients;

        private ClientPool _subject;


        [TestInitialize]
        public void Initialize()
        {
            _client = Substitute.For<IClientProxy>();
            _hubContext = Substitute.For<IHubContext<DatabaseApiHub>>();
            _hubClients = Substitute.For<IHubClients>();

            _hubContext.Clients.Returns(_hubClients);
            _hubClients.Clients(Arg.Is<string[]>(a => a[0] == ConnectionId)).Returns(_client);

            _subject = new ClientPool(_hubContext);
        }


        [TestMethod]
        public void InvokeAsync_GivenMethodAndArguments_InvokesSuchMethodOnCorrespondingClient()
        {
            // Given the arguments to pass to the invocation
            const string queryName = "query#1";
            const string methodName = ClientMethods.EntityChanged;
            IEntity changedEntity = Stubs.NewEntity;
            ExternalChange[] changes =
            {
                new(changedEntity, ChangeType.Delete)
            };

            // When commanded to invoke the method
            _subject.InvokeMethodAsync(methodName, ConnectionId, queryName, changes).WaitFor();

            // Then
            _client
                .Received()
                .SendCoreAsync(
                    methodName,
                    Arg.Is<object[]>(o =>
                        (string)o[0] == queryName &&
                        ((ExternalChange[])o[1])[0] == new ExternalChange(changedEntity, ChangeType.Delete)),
                    Arg.Any<CancellationToken>());
        }
    }
}