using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QualityGate.RealTime.Notifications;
using QualityGate.RealTime.Queries;
using QualityGate.RealTime.Changes;
using QualityGate.RealTime.Domain;
using Raven.Client.Documents;
using Raven.Client.Documents.Changes;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace QualityGate.RealTime
{
    /// <summary>
    ///     Contains extensions to easily setup the Real time data synchronization in ASPNET Core services.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class SetupExtensions
    {
        /// <summary>
        ///     Configures all the dependencies in the ASPNET Core service to connect data directly from the browser
        ///     (through websockets) to a RavenDB database.
        /// </summary>
        /// <param name="services">
        ///     Services collection provided by ASPNET Core to configure the required types for the mentioned in the
        ///     summary to happen.
        /// </param>
        /// <param name="configuration">
        ///     Application configuration to extract RavenDB connection information from.
        /// </param>
        public static void AddRealTime(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSignalR();

            services.AddSingleton<IQueryRepository, QueryRepository>();
            services.AddTransient<IEntityRepository, EntityRepository>();
            services.AddSingleton<ChangeObserver>();
            services.AddTransient<IChangeNotifier, ChangeNotifier>();
            services.AddTransient<IClientPool, ClientPool>();

            services.AddRavenDbStore(configuration);
        }

        /// <summary>
        ///     Includes to the given ASPNET Core application the capabilities allowing it to listen and notify changes
        ///     in domain entities stored in the configured RavenDB database.
        /// </summary>
        /// <param name="app">
        ///     Application builder that allows the configuration of the ASPNET Core app in the way mentioned in the
        ///     summary.
        /// </param>
        public static void UseRealTime(this IApplicationBuilder app)
        {
            var documentStore = app.ApplicationServices.GetService<IDocumentStore>()!;
            var subscription = documentStore.Changes();
            Task.WaitAll(subscription.EnsureConnectedNow());
            IChangesObservable<DocumentChange> allDocumentsSubscription = subscription.ForAllDocuments();
            var changeObserver = app.ApplicationServices.GetService<ChangeObserver>()!;
            allDocumentsSubscription.Subscribe(changeObserver);
        }

        private static void AddRavenDbStore(this IServiceCollection services, IConfiguration configuration)
        {
            var url = configuration["Database:Url"];
            var database = configuration["Database:Name"];

            services.AddSingleton<IDocumentStore>(_ =>
            {
                var documentStore = new DocumentStore
                {
                    Urls = new[] { url },
                    Database = database,
                    Conventions = { IdentityPartsSeparator = '_' }
                };
                documentStore.Initialize();

                // Make sure the database exists before it's first use
                EnsureDatabaseExists(documentStore);

                return documentStore;
            });
            services.AddScoped<IAsyncDocumentSession>(serviceProvider =>
            {
                var store = serviceProvider.GetService<IDocumentStore>();
                var session = store!.OpenAsyncSession();

                return session;
            });
        }

        private static void EnsureDatabaseExists(IDocumentStore store)
        {
            var database = store.Database;

            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("Store's database field cannot be null or whitespace.", nameof(store));

            try
            {
                store.Maintenance.ForDatabase(database).Send(new GetStatisticsOperation());
            }
            catch (DatabaseDoesNotExistException)
            {
                store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(database)));
                Console.WriteLine("Database created (it did not exist)");
            }
        }
    }
}