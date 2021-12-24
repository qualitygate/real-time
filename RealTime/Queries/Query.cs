using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using QualityGate.RealTime.Changes;

namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     Represents the definition of query that a client (a browser app) is interested in listening for changes
    ///     experienced by the entities satisfying such query.
    /// </summary>
    /// <param name="ConnectionId">
    ///     Identifies the opened connection to the client interested in the query's changed entities.
    /// </param>
    /// <param name="Name">Name that identifiers the query.</param>
    /// <param name="Table">Name of the table where there are stored the entities to query.</param>
    public record Query(string ConnectionId, string Name, string Table)
    {
        /// <summary>
        ///     Gets the conditions in the current query. If null, no conditions are evaluated.
        /// </summary>
        public Condition[]? Conditions { get; [ExcludeFromCodeCoverage] init; }

        /// <summary>
        ///     Gets or sets the entities Fields to return as result of the query (this is to allows entity projection).
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string[]? Fields { get; [ExcludeFromCodeCoverage] init; }

        /// <inheritdoc cref="QueryDto.OrderBy"/>
        public OrderBy? OrderBy { get; set; }


        /// <summary>
        ///     Determines whether the provided entity change matches the query criteria.
        /// </summary>
        /// <param name="change">Entity change to evaluate whether it matches the query criteria.</param>
        /// <returns>
        ///     True if the given entity change is relevant to this query; false otherwise.
        /// </returns>
        public bool MatchesChange(Change change)
        {
            return Table == change.Table && Conditions is null || Conditions?.All(c => c.Match(change)) == true;
        }

        /// <summary>
        ///     Implicitly converts the given query to the corresponding RQL string.
        /// </summary>
        /// <param name="query">Query to get its string representation.</param>
        /// <returns>
        ///     Returns the entry representation of the current Query.
        /// </returns>
        public static implicit operator string(Query query) => query.ToString();

        /// <summary>
        ///     Gets the string representation of the current Query in RQL language.
        /// </summary>
        /// <returns>A string representing the given query.</returns>
        public override string ToString()
        {
            // Coming from the minimum RQL query expression
            var builder = new StringBuilder($"from {Table}");

            // Add the conditions if they are present
            if (Conditions?.Length > 0)
            {
                string Accumulator(string sentence, Condition condition) => sentence + $" {condition.ToRql()}";
                var whereSentence = Conditions.Aggregate(" where", Accumulator);

                builder.Append(whereSentence);
            }

            // Add the order by sentence
            if (OrderBy?.Fields?.Count() > 0)
            {
                var fields = string.Join(", ", OrderBy.Fields);
                var ascending = OrderBy.Ascending ? "" : " desc";
                var orderBySentence = $" order by {fields}{ascending}";
                builder.Append(orderBySentence);
            }

            // Add projection sentence if Fields is defined and not empty
            if (Fields?.Length > 0) builder.Append($" select {string.Join(", ", Fields)}");

            return builder.ToString();
        }
    }
}