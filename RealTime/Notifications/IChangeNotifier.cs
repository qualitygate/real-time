using System.Threading.Tasks;
using QualityGate.RealTime.Queries;
using QualityGate.RealTime.Changes;
using Raven.Client.Documents.Changes;

namespace QualityGate.RealTime.Notifications
{
    /// <summary>
    ///     Describes the contract of a RavenDB entities change notifier.
    /// </summary>
    /// <remarks>
    ///     When a client (a website) register a query, the registering entity will call this class's
    ///     <see cref="NotifyFirstTime"/> method, which will execute the specified query and notify to the client the
    ///     results.
    ///     When a <see cref="ChangeObserver"/> gets notified that an entity changed, it will invoke this class
    ///     <see cref="Notify"/> method, which will in turn notify the clients (browser websites) having queries that
    ///     the change entity satisfies, so they can reflect the entity's changes in their website.
    /// </remarks>
    public interface IChangeNotifier
    {
        /// <summary>
        ///     Notifies the initial results of the given query. Use this method the first time a query is defined.
        /// </summary>
        /// <param name="query">
        ///     Query that was registered, and that must be executed to send its results to its corresponding client.
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> that asynchronously performs the query execution and results notification.
        /// </returns>
        Task NotifyFirstTime(Query query);

        /// <summary>
        ///     Call this method when an entity gets changed, to notify the interested clients about it.
        /// </summary>
        /// <param name="documentChange">
        ///     A description of the entity change in terms of RavenDB DSL.
        /// </param>
        /// <remarks>
        ///     This method will filter which queries are satisfied by the changed entity. Found the queries, their
        ///     corresponding clients will then get notified that the given entity has changed, so they can reflect
        ///     those changes.
        /// </remarks>
        /// <returns>
        ///     A <see cref="Task"/> that asynchronously performs the notification to the corresponding queries.
        /// </returns>
        Task Notify(DocumentChange documentChange);
    }
}