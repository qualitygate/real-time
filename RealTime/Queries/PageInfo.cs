namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     Represents the definition of the results of a paginated query.
    /// </summary>
    /// <typeparam name="T">Type of the entities being the result of a query.</typeparam>
    /// <param name="Total">Total entities in the database of type <typeparamref name="T"/>.</param>
    /// <param name="Items">Entities being the results of a query.</param>
    /// <param name="Page">Number of the page of entities to return.</param>
    /// <param name="Size">Size of the page of entities to return.</param>
    public record PageInfo<T>(long Total, T[] Items, int Page, int Size);
}
