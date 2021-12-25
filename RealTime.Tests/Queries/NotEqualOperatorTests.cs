using Microsoft.VisualStudio.TestTools.UnitTesting;
using QualityGate.RealTime.Queries;

namespace QualityGate.RealTime.Tests.Queries
{
    [TestClass]
    public class NotEqualOperatorTests
    {
        private readonly NotEqualOperator _subject = new();


        [TestMethod]
        public void Evaluate_BothValuesAreNull_ReturnsFalse()
        {
            // When
            var result = _subject.Evaluate(null, null);

            // Then
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Evaluate_ExpectedValueIsNull_ReturnsTrue()
        {
            // When
            var result = _subject.Evaluate(null, 1);

            // Then
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Evaluate_ActualValueIsNull_ReturnsTrue()
        {
            // When
            var result = _subject.Evaluate(1, null);

            // Then
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Evaluate_BothReferenceValuesAreSameObject_ReturnsFalse()
        {
            // Given
            var value = new { };

            // When
            var result = _subject.Evaluate(value, value);

            // Then
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Evaluate_BothValuesAreSame_ReturnsFalse()
        {
            // When
            var result = _subject.Evaluate(1, 1);

            // Then
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Evaluate_BothValuesAreNotSame_ReturnsTrue()
        {
            // When
            var result = _subject.Evaluate(1, 2);

            // Then
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ToRql_ReturnsCorrectEqualStatement()
        {
            // When
            var statement = _subject.ToRql("Name", "'Peter'");

            // Then
            Assert.AreEqual("Name <> 'Peter'", statement);
        }
    }
}