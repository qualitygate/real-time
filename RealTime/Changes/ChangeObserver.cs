using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using QualityGate.RealTime.Notifications;
using Raven.Client.Documents.Changes;

namespace QualityGate.RealTime.Changes
{
    /// <summary>
    ///     Used to be registered in the change notifier interface of RavenDB. With a single instance it's enough.
    ///     It listens to changes on entities, and takes the responsibility of propagating those changes over the
    ///     infrastructure that lets connected browser clients know.
    /// </summary>
    // ReSharper disable once UnusedType.Global
    public class ChangeObserver : IObserver<DocumentChange>
    {
        private readonly ILogger<ChangeObserver> _logger;
        private readonly IChangeNotifier _notifier;


        /// <summary>
        ///     Initializes a new instance of <see cref="ChangeObserver"/> with a logger and a query notifier.
        /// </summary>
        /// <param name="logger">Used to log events occurring in this instance.</param>
        /// <param name="notifier">Notifies the observed changes</param>
        public ChangeObserver(ILogger<ChangeObserver> logger, IChangeNotifier notifier)
        {
            _logger = logger;
            _notifier = notifier;
        }


        /// <summary>
        ///     Invoked when the listening of this instance has ended.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public void OnCompleted()
        {
            _logger.LogDebug("Finished listening changes");
        }

        /// <summary>
        ///     Invoked when an error prevents this observer instance from working properly
        /// </summary>
        /// <param name="error">The actual <see cref="Exception"/> that represents the error.</param>
        [ExcludeFromCodeCoverage]
        public void OnError(Exception error)
        {
            _logger.LogError(error, "Problem during change listening");
        }

        /// <summary>
        ///     Invoked when a change was notified by RavenDB. This method submits the change over the DataSync change
        ///     notification infrastructure to let the listening browsers client know.
        /// </summary>
        /// <param name="change">
        ///     This is the description of the change occurred to an entity in RavenDB.
        /// </param>
        public async void OnNext(DocumentChange change)
        {
            _logger.LogDebug($"Table: {change.CollectionName}, Type: {change.Type}, Id: {change.Id}");
            await _notifier.NotifyEntityChanged(change);
        }
    }
}