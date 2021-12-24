using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using QualityGate.RealTime.Changes;
using QualityGate.RealTime.Utils;

namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     Default implementation of <see cref="IQueryRepository"/> interface. See its docs to learn more details.
    /// </summary>
    public class QueryRepository : IQueryRepository, IEnumerable<Query>
    {
        private readonly ILogger<QueryRepository> _logger;
        private readonly ConcurrentDictionary<QueryKey, Query> _queries = new();


        /// <summary>
        ///     Initializes a new instance of <see cref="QueryRepository"/> given a logger.
        /// </summary>
        /// <param name="logger">Used to log events occurring in this instance.</param>
        public QueryRepository(ILogger<QueryRepository> logger)
        {
            _logger = logger;
        }


        /// <inheritdoc cref="IQueryRepository.AddQuery"/>
        public void AddQuery(Query query)
        {
            _logger.LogDebug($"Adding Query:\n{query.ToJson()}");
            var key = QueryKey.FromQuery(query);
            _queries.TryAdd(key, query);
            _logger.LogDebug($"Query: {key} added");
        }

        /// <inheritdoc cref="IQueryRepository.ModifyQuery"/>
        public void ModifyQuery(Query query)
        {
            _logger.LogDebug($"Modify Query:\n{query.ToJson()}");
            var key = QueryKey.FromQuery(query);
            _queries[key] = query;
            _logger.LogDebug($"Query: {key} modify");
        }

        /// <inheritdoc cref="IQueryRepository.RemoveAllQueries"/>
        public void RemoveAllQueries(string connectionId)
        {
            var deadQueries = _queries
                .Where(x => x.Key.ConnectionId == connectionId)
                .Select(x => x.Key)
                .ToArray();

            _logger.LogDebug($"Removing: {deadQueries.Length} queries");
            foreach (var query in deadQueries) _queries.Remove(query, out _);
        }

        /// <inheritdoc cref="IQueryRepository.RemoveQuery"/>
        public void RemoveQuery(Query query)
        {
            var key = QueryKey.FromQuery(query);
            _queries.Remove(key, out _);
        }

        /// <inheritdoc cref="IQueryRepository.SelectMatching"/>
        public Query[] SelectMatching(Change change)
        {
            return _queries
                .Where(x => x.Value.MatchesChange(change))
                .Select(x => x.Value)
                .ToArray();
        }

        /// <inheritdoc cref="IQueryRepository.SelectMatchingTable"/>
        public Query[] SelectMatchingTable(Change change)
        {
            return _queries
                .Where(x => x.Value.Table == change.Table)
                .Select(x => x.Value)
                .ToArray();
        }

        #region IEnumerable<Query>

        public IEnumerator<Query> GetEnumerator()
        {
            return _queries.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion


        private record QueryKey(string Name, string ConnectionId)
        {
            public static QueryKey FromQuery(Query query) => new(query.Name, query.ConnectionId);

            public override string ToString()
            {
                return $"{Name} | {ConnectionId}";
            }
        }
    }
}