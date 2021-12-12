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
        /// <param name="logger">Used to log events occuring in this instance.</param>
        /// <param name="queryRepository">Repository that allows to obtain, register and unregister queries.</param>
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
        public async Task AddQuery(QueryDto queryDto)
        {
            _logger.LogDebug($"AddQuery, Name: {queryDto.Name}");

            var query = queryDto.ToQuery(Context.ConnectionId);
            _queryRepository.AddQuery(query);

            _logger.LogDebug("Sending first results");
            await _notifier.NotifyFirstTime(query);
        }

        /// <summary>
        ///     Unregisters the query matching the given definition.
        /// </summary>
        /// <param name="queryDto">DTO bringing the details of the query to register.</param>
        public Task RemoveQuery(QueryDto queryDto) => Task.Factory.StartNew(() =>
        {
            var query = queryDto.ToQuery(Context.ConnectionId);
            _queryRepository.RemoveQuery(query);
        });
    }
}