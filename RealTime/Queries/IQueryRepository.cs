using QualityGate.RealTime.Changes;

namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     Repository that stores the registered queries by clients (browser webapps).
    /// </summary>
    /// <remarks>
    ///     The repository is capable to add/remove in thread-sage manner queries to it. It also allows bulk removal
    ///     of all the queries associated to a specific client connection given its identifier. Also allows selection
    ///     of impacted queries by a certain entity change. See its methods descriptions to learn more.
    /// </remarks>
    public interface IQueryRepository
    {
        /// <summary>
        ///     Adds in a thread-safe manner the given query.
        /// </summary>
        /// <param name="query">Query to add.</param>
        void AddQuery(Query query);

        /// <summary>
        ///     Removes in a thread-safe fashion all queries associated to the specified connection.
        /// </summary>
        /// <param name="connectionId">Identifier of the connection to remove all of its associated queries.</param>
        void RemoveAllQueries(string connectionId);

        /// <summary>
        ///     Removes in a thread-safe manner the specified query.
        /// </summary>
        /// <param name="query">Query to remove.</param>
        void RemoveQuery(Query query);

        /// <summary>
        ///     Selects the queries impacted by the specified entity change.
        /// </summary>
        /// <param name="change">A change that occurred in a certain entity.</param>
        /// <returns>An Array containing the impacted queries by the specified <paramref name="change"/>.</returns>
        Query[] SelectMatching(Change change);

        /// <summary>
        ///     Selects the queries that match entities of the given changed entity type.
        /// </summary>
        /// <param name="change">A change bringing the changed entity.</param>
        /// <returns>Those queries which Table field matches the changed entity database Table name.</returns>
        Query[] SelectMatchingTable(Change change);
    }
}