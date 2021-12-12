using System;
using System.Collections.Generic;
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
        /// <param name="logger">Used to log events occuring in this instance.</param>
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


        /// <inheritdoc cref="IChangeNotifier.NotifyFirstTime"/>
        public async Task NotifyFirstTime(Query query)
        {
            _logger.LogDebug($"Searching entities to satisfy the query: {query.Name} for client: {query.ConnectionId}");
            var entities = await _entityRepository.Find<IEntity>(query);

            _logger.LogDebug($"Found {entities.Length} entities satisfying the query, notifying connected clients");
            var changes = entities.Select(e => new Change(e, query.Table, ChangeType.Upsert));
            await Notify(changes, query);
        }

        /// <inheritdoc cref="IChangeNotifier.Notify"/>
        public async Task Notify(DocumentChange documentChange)
        {
            if (documentChange.CollectionName.StartsWith("@hilo")) return;

            var change = await GetChangeType(documentChange);

            _logger.LogDebug("Loading changed entity from database");
            Query[] matchingQueriesByConditions = await NotifyMatchingQueriesByConditions(change);

            Query[] queriesMatchingEntityTableButNotEntity =
                await NotifyQueriesMatchingTableButNotMatchingEntityAnymore(change, matchingQueriesByConditions);

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
                var entity = await _entityRepository.Find<IEntity>(documentChange.Id);
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

        private async Task Notify(IEnumerable<Change> changes, params Query[] queries)
        {
            var externalChanges = changes.Select(ExternalChange.FromChange).ToArray();

            foreach (var (connectionId, name, _) in queries)
                await _clients.InvokeMethodAsync(ClientMethods.EntityChanged, connectionId, name, externalChanges);
        }

        private async Task<Query[]> NotifyQueriesMatchingTableButNotMatchingEntityAnymore(
            Change change, Query[] matchingQueriesByConditions)
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

            _logger.LogDebug($"Entity of type: {entity.GetType().Name} with Id: {entity.Id}, " +
                             $"suffered a {change.Type.ToString()} operation");

            Query[] matchingQueriesByConditions = _queryRepository.SelectMatching(change);
            await Notify(change, matchingQueriesByConditions);

            return matchingQueriesByConditions;
        }

        #endregion
    }
}