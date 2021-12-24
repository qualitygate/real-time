using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using QualityGate.RealTime.Queries;
using QualityGate.RealTime.Notifications;
using QualityGate.RealTime.Utils;

namespace QualityGate.RealTime
{
    /// <summary>
    ///     SignalR HUB used to expose functionality to browser websockets in order to allow them to declare and
    ///     register queries, through which they will be notified when any of the entities matching those queries
    ///     change.
    /// </summary>
    public class DatabaseApiHub : Hub
    {
        private readonly ILogger<DatabaseApiHub> _logger;
        private readonly IQueryRepository _queryRepository;
        private readonly IChangeNotifier _notifier;


        /// <summary>
        ///     Initializes a new instance of <see cref="DatabaseApiHub"/> class given a logger, entity repository
        ///     and a change notifier.
        /// </summary>
        /// <param name="logger">Used to log events occurring in this instance.</param>
        /// <param name="queryRepository">Repository that allows to obtain, register and un-register queries.</param>
        /// <param name="notifier">
        ///     An object used to notify changes in entities satisfying any of the registered queries.
        /// </param>
        public DatabaseApiHub(
            ILogger<DatabaseApiHub> logger,
            IQueryRepository queryRepository,
            IChangeNotifier notifier)
        {
            _logger = logger;
            _queryRepository = queryRepository;
            _notifier = notifier;
        }


        /// <summary>
        ///     Called by SignalR engine when a connection disconnects by an exception. It removes all queries
        ///     associated to the broken connection.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> that broke the connection.</param>
        /// <returns>
        ///     A <see cref="Task"/> that does the removal of the disassociated queries.
        /// </returns>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogError(exception, $"Disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
            _logger.LogWarning($"Removing queries associated to: {Context.ConnectionId} connection");
            _queryRepository.RemoveAllQueries(Context.ConnectionId);
        }


        /// <summary>
        ///     Registers a query given its data.
        /// </summary>
        /// <param name="queryDto">DTO bringing the details of the query to register.</param>
        /// <returns>A <see cref="Task"/> that asynchronously does the registration.</returns>
        public async Task AddQuery(QueryDto queryDto)
        {
            _logger.LogDebug($"AddQuery, Name: {queryDto.Name}");

            var query = queryDto.ToQuery(Context.ConnectionId);
            _queryRepository.AddQuery(query);

            _logger.LogDebug("Sending first results");
            await _notifier.NotifyFullResults(query);
        }

        /// <summary>
        ///     Modifies an existing query with a new definition of itself and notifies again the results.
        /// </summary>
        /// <param name="queryDto">New definition to apply to an existing query</param>
        /// <returns></returns>
        public async Task ModifyQuery(QueryDto queryDto)
        {
            _logger.LogDebug($"ModifyQuery, Name: {queryDto.Name}");

            var query = queryDto.ToQuery(Context.ConnectionId);
            _queryRepository.ModifyQuery(query);

            await _notifier.NotifyFullResults(query);
        }

        /// <summary>
        ///     Un-registers the query matching the given definition.
        /// </summary>
        /// <param name="queryDto">DTO bringing the details of the query to register.</param>
        public Task RemoveQuery(QueryDto queryDto) => Task.Factory.StartNew(() =>
        {
            var query = queryDto.ToQuery(Context.ConnectionId);
            _queryRepository.RemoveQuery(query);
        });
    }
}