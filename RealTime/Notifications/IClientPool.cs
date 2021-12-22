using System.Threading.Tasks;

namespace QualityGate.RealTime.Notifications
{
    /// <summary>
    ///     Describes the contract of the facade that allows to invoke methods on the connected SignalR clients
    ///     (browser web-apps). Implementors of this interface should be used to notify entities changes in a RavenDB
    ///     database.
    /// </summary>
    public interface IClientPool
    {
        /// <summary>
        ///     Invokes a given method on the client(s) having the provided connection id, passing required arguments.
        /// </summary>
        /// <param name="method">Name of the method to invoke.</param>
        /// <param name="connectionId">
        ///     Identifies the client's which connection will be used to send the invocation.
        /// </param>
        /// <param name="queryName">Name of the query where entities that satisfy it have changed.</param>
        /// <param name="changes">Set of changes, expressing which entity experienced what change</param>
        /// <returns>A <see cref="Task"/> that asynchronously performs the invocation.</returns>
        Task InvokeMethodAsync(string method, string connectionId, string queryName, params object[] changes);
    }
}