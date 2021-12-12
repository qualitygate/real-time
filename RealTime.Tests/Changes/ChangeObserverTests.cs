using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using QualityGate.RealTime.Changes;
using QualityGate.RealTime.Notifications;
using Raven.Client.Documents.Changes;

namespace QualityGate.RealTime.Tests.Changes
{
    [TestClass]
    public class ChangeObserverTests
    {
        private ILogger<ChangeObserver> _logger;
        private IChangeNotifier _changeNotifier;

        private ChangeObserver _subject;


        [TestInitialize]
        public void Initialize()
        {
            _logger = Substitute.For<ILogger<ChangeObserver>>();
            _changeNotifier = Substitute.For<IChangeNotifier>();

            _subject = new ChangeObserver(_logger, _changeNotifier);
        }


        [TestMethod]
        public void OnNext_GivenDocumentChange_PropagatesItToTheNotifier()
        {
            var change = new DocumentChange
            {
                Id = "1",
                Type = DocumentChangeTypes.Put,
                ChangeVector = "",
                CollectionName = "CollectionA"
            };

            _changeNotifier.Notify(change).Returns(Task.CompletedTask);

            // When
            _subject.OnNext(change);

            // Then
            _changeNotifier.Received().Notify(change);
        }
    }
}