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


        /// <inheritdoc cref="IEntityRepository.FindPageAsync{T}"/>
        public async Task<PageInfo<T>> FindPageAsync<T>(PaginatedQuery query)
        {
            var session = _documentStore.OpenAsyncSession();

            var entities = await session.Advanced.AsyncRawQuery<T>(query)
                .Skip(query.Size * query.Page)
                .Take(query.Size)
                .ToArrayAsync();
            var total = await session.Query<object>(collectionName: query.Table).CountAsync();

            return new PageInfo<T>(total, entities, query.Page, query.Size);
        }

        /// <inheritdoc cref="FindAllAsync{T}"/>
        public async Task<T[]> FindAllAsync<T>(Query query)
        {
            var session = _documentStore.OpenAsyncSession();
            return await session.Advanced.AsyncRawQuery<T>(query).ToArrayAsync();
        }

        /// <inheritdoc cref="IEntityRepository.FindAsync{T}"/>
        public async Task<T> FindAsync<T>(string id)
        {
            var session = _documentStore.OpenAsyncSession();
            return await session.LoadAsync<T>(id);
        }
    }
}