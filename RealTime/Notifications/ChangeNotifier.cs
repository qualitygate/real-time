using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QualityGate.RealTime.Queries;
using QualityGate.RealTime.Changes;
using QualityGate.RealTime.Domain;
using Raven.Client.Documents.Changes;

namespace QualityGate.RealTime.Notifications
{
    /// <summary>
    ///     Default implementation of <see cref="IChangeNotifier"/> interface. See its docs for more information.
    /// </summary>
    // ReSharper disable once UnusedType.Global
    public class ChangeNotifier : IChangeNotifier
    {
        private readonly IEntityRepository _entityRepository;
        private readonly ILogger<ChangeNotifier> _logger;
        private readonly IClientPool _clients;
        private readonly IQueryRepository _queryRepository;


        /// <summary>
        ///     Initializes a new instance of <see cref="ChangeNotifier"/> class given a document store, SignalR HUB
        ///     context, query repository and a logger.
        /// </summary>
        /// <param name="clients">
        ///     Pool of SignalR connected clients, the ones that are interested in entities changes.
        /// </param>
        /// <param name="queryRepository">
        ///     Repository where all the client browsers registered queries are stored.
        /// </param>
        /// <param name="logger">Used to log events occurring in this instance.</param>
        /// <param name="entityRepository">
        ///     Repository to easily fetch changed entities from RavenDB.
        /// </param>
        public ChangeNotifier(
            IClientPool clients,
            IQueryRepository queryRepository,
            ILogger<ChangeNotifier> logger,
            IEntityRepository entityRepository)
        {
            _clients = clients;
            _queryRepository = queryRepository;
            _logger = logger;
            _entityRepository = entityRepository;
        }


        /// <inheritdoc cref="IChangeNotifier.NotifyFullResults"/>
        public async Task NotifyFullResults(Query query)
        {
            if (query is PaginatedQuery paginatedQuery)
            {
                await NotifyPageQueryChanged(paginatedQuery);
            }
            else
            {
                _logger.LogDebug($"Searching entities to satisfy the query: {query.Name} for client: {query.ConnectionId}");
                var entities = await _entityRepository.FindAllAsync<object>(query);

                _logger.LogDebug($"Found {entities.Length} entities satisfying the query, notifying connected clients");
                var changes = entities.Select(e => new Change(e, query.Table, ChangeType.Upsert)).ToArray();
                await Notify(changes, query);
            }
        }

        /// <inheritdoc cref="IChangeNotifier.NotifyEntityChanged"/>
        public async Task NotifyEntityChanged(DocumentChange documentChange)
        {
            if (documentChange.CollectionName.StartsWith("@hilo")) return;

            var change = await GetChangeType(documentChange);

            _logger.LogDebug("Loading changed entity from database");
            Query[] matchingQueriesByConditions = await NotifyMatchingQueriesByConditions(change);
            Query[] queriesMatchingEntityTableButNotEntity = await NotifyQueriesMatchingTableButNotMatchingEntityAnymore(change, matchingQueriesByConditions);

            var affectedQueries = matchingQueriesByConditions.Length + queriesMatchingEntityTableButNotEntity.Length;
            _logger.LogDebug($"Affected {affectedQueries} queries. Notifying impacted clients");
        }


        #region Internals

        private async Task<Change> GetChangeType(DocumentChange documentChange)
        {
            var table = documentChange.CollectionName;
            Change change;

            if (documentChange.Type
                is DocumentChangeTypes.Delete
                or DocumentChangeTypes.DeleteOnTombstoneReplication)
            {
                change = new Change(new DeletedEntity(documentChange.Id), table, ChangeType.Delete);
            }
            else
            {
                var entity = await _entityRepository.FindAsync<object>(documentChange.Id);
                var changeType = documentChange.Type switch
                {
                    DocumentChangeTypes.Put => ChangeType.Upsert,
                    _ => throw new NotImplementedException()
                };
                change = new Change(entity, table, changeType);
            }

            return change;
        }

        private Task Notify(Change change, params Query[] queries) => Notify(new[] { change }, queries);

        private async Task Notify(Change[] changes, params Query[] queries)
        {
            foreach (var query in queries)
            {
                if (query is PaginatedQuery pagedQuery)
                {
                    await NotifyPageQueryChanged(pagedQuery);
                }
                else
                {
                    var externalChanges = changes.Select(ExternalChange.FromChange);

                    await _clients.InvokeMethodAsync(
                        ClientMethods.EntityChanged,
                        query.ConnectionId,
                        query.Name,
                        externalChanges.ToArray());
                }
            }
        }

        private async Task NotifyPageQueryChanged(PaginatedQuery query)
        {
            var pageInfo = await _entityRepository.FindPageAsync<object>(query);

            await _clients.InvokeMethodAsync(
                ClientMethods.PageChanged,
                query.ConnectionId,
                query.Name,
                pageInfo);
        }

        private async Task<Query[]> NotifyQueriesMatchingTableButNotMatchingEntityAnymore(
            Change change,
            Query[] matchingQueriesByConditions)
        {
            Query[] queriesMatchingTableButNotChange = _queryRepository
                .SelectMatchingTable(change)
                .Except(matchingQueriesByConditions)
                .Where(q => !q.MatchesChange(change))
                .ToArray();

            var deletionChange = change with { Type = ChangeType.Delete };
            await Notify(deletionChange, queriesMatchingTableButNotChange);

            return queriesMatchingTableButNotChange;
        }

        private async Task<Query[]> NotifyMatchingQueriesByConditions(Change change)
        {
            var entity = change.Entity;

            _logger.LogDebug($"Entity of type: {entity.GetType().Name}, suffered a {change.Type} operation");

            Query[] matchingQueriesByConditions = _queryRepository.SelectMatching(change);
            await Notify(change, matchingQueriesByConditions);

            return matchingQueriesByConditions;
        }

        #endregion
    }
}