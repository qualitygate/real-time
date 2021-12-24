namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     Represents the definition of a <see cref="Query"/> which results are paginated.
    /// </summary>
    /// <param name="ConnectionId">
    ///     Identifies the opened connection to the client interested in the query's changed entities.
    /// </param>
    /// <param name="Name">Name that identifiers the query.</param>
    /// <param name="Table">Name of the table where there are stored the entities to query.</param>
    public record PaginatedQuery(string ConnectionId, string Name, string Table) : Query(ConnectionId, Name, Table)
    {
        /// <summary>
        ///     Initializes a new instance of <see cref="PaginatedQuery"/>.
        /// </summary>
        /// <param name="connectionId">
        ///     Identifies the opened connection to the client interested in the query's changed entities.
        /// </param>
        /// <param name="name">Name that identifiers the query.</param>
        /// <param name="table">Name of the table where there are stored the entities to query.</param>
        /// <param name="page">Number of the page to return.</param>
        /// <param name="size">Size of the page to return.</param>
        public PaginatedQuery(string connectionId, string name, string table, int page, int size) : this(connectionId, name, table)
        {
            Page = page;
            Size = size;
        }

        /// <summary>
        ///     Gets or sets the number of the slice of elements to fetch.
        /// </summary>
        public int Page { get; init; }

        /// <summary>
        ///     Gets or sets the size of the slice of elements to fetch.
        /// </summary>
        public int Size { get; init; }

        /// <summary>
        ///     Gets RQL representation of the the current paginated query.
        /// </summary>
        /// <returns>Returns the RQL representation of the paginated query.</returns>
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
