using System.Collections.Generic;

namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     Represents a query definition as received from browser web-apps.
    /// </summary>
    public record QueryDto
    {
        /// <summary>
        ///     Gets or sets the enumerable of query conditions.
        /// </summary>
        public IEnumerable<ConditionDto>? Conditions { get; init; }

        /// <summary>
        ///     Gets or sets the fields that the query should return of each of the entities. Defaults to an array with
        ///     a single string element: "*", signaling that all fields must be returned.
        /// </summary>
        public string[] Fields { get; init; } = { "*" };

        /// <summary>
        ///     Gets or sets the Name of the query.
        /// </summary>
        public string Name { get; init; } = "?";

        /// <summary>
        ///     Gets or sets the optional definition of the ordering of the query results.
        /// </summary>
        public OrderBy? OrderBy { get; init; }

        /// <summary>
        ///     Gets or sets the Name of the table to get entities from.
        /// </summary>
        public string Table { get; init; } = "?";

        /// <summary>
        ///     Gets or sets the number of the slice of elements to fetch.
        /// </summary>
        public int? Page { get; init; }

        /// <summary>
        ///     Gets or sets the size of the slice of elements to fetch.
        /// </summary>
        public int? Size { get; init; }
    }
}