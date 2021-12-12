using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using QualityGate.RealTime.Changes;

namespace QualityGate.RealTime.Notifications
{
    /// <summary>
    ///     Default implementation of <see cref="IClientPool"/> interface. See its docs for more details.
    /// </summary>
    public class ClientPool : IClientPool
    {
        private readonly IHubContext<DatabaseApiHub> _hubContext;


        /// <summary>
        ///     Initializes a new instance of <see cref="ClientPool"/> class given a SignalR hub context.
        /// </summary>
        /// <param name="hubContext">
        ///     SignalR hub context, used to notify listening client browsers about entities.
        /// </param>
        public ClientPool(IHubContext<DatabaseApiHub> hubContext)
        {
            _hubContext = hubContext;
        }


        /// <inheritdoc cref="IClientPool.InvokeMethodAsync"/>
        public Task InvokeMethodAsync(
            string method,
            string connectionId,
            string queryName,
            params ExternalChange[] changes)
        {
            var client = _hubContext.Clients.Clients(connectionId);

            return client.SendAsync(method, queryName, changes);
        }
    }
}