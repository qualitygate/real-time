using System.Threading.Tasks;
using QualityGate.RealTime.Queries;

namespace QualityGate.RealTime.Domain
{
    /// <summary>
    ///     Describes the contract of a repository that allows to access data of domain entities in a RavenDB database.
    /// </summary>
    public interface IEntityRepository
    {
        /// <summary>
        ///     Gets the entities matching the given query.
        /// </summary>
        /// <param name="query">Query to evaluate while searching for entities.</param>
        /// <typeparam name="T">Type of the entities to query.</typeparam>
        /// <returns>
        ///     A <see cref="Task"/> that asynchronously does the search and will eventually contain the entities
        ///     satisfying the specified <paramref name="query"/>.
        /// </returns>
        Task<T[]> Find<T>(Query query);

        /// <summary>
        ///     Gets the single entity identified the given id.
        /// </summary>
        /// <param name="id">Identifier of the entity to obtain.</param>
        /// <typeparam name="T">Type of the entity to obtain.</typeparam>
        /// <remarks>
        ///     It always brings the current version from the database effectively avoiding any caching mechanism.
        /// </remarks>
        /// <returns>
        ///     A <see cref="Task"/> that asynchronously does the search and will eventually contain the entity
        ///     containing the specified <paramref name="id"/>.
        /// </returns>
        Task<T> Find<T>(string id);
    }
}