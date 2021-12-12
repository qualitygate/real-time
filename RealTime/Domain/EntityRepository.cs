using System.Threading.Tasks;
using QualityGate.RealTime.Queries;
using Raven.Client.Documents;

namespace QualityGate.RealTime.Domain
{
    /// <summary>
    ///     Default implementation of <see cref="IEntityRepository"/> interface. See its docs for more details.
    /// </summary>
    public class EntityRepository : IEntityRepository
    {
        private readonly IDocumentStore _documentStore;


        /// <summary>
        ///     Initializes a new instance of <see cref="EntityRepository"/> class with a document store.
        /// </summary>
        /// <param name="documentStore">RavenDB document store. Used to fetch the entity that changed.</param>
        public EntityRepository(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }


        /// <inheritdoc cref="IEntityRepository.Find{T}(QualityGate.RealTime.Queries.Query)"/>
        public async Task<T[]> Find<T>(Query query)
        {
            using var session = _documentStore.OpenAsyncSession();
            return await session.Advanced.AsyncRawQuery<T>(query).ToArrayAsync();
        }

        /// <inheritdoc cref="IEntityRepository.Find{T}(string)"/>
        public async Task<T> Find<T>(string id)
        {
            using var session = _documentStore.OpenAsyncSession();
            return await session.LoadAsync<T>(id);
        }
    }
}