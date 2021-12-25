using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QualityGate.RealTime.Queries;

namespace QualityGate.RealTime.Tests.Queries
{
    [TestClass]
    public class MatchesOperatorTests
    {
        private readonly MatchesOperator _subject = new();


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Evaluate_ExpectedValueIsNull_ThrowsException() => _subject.Evaluate(null, null);

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Evaluate_ActualValueIsNull_ThrowsException() => _subject.Evaluate("Name", null);

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Evaluate_ExpectedValueIsEmptyString_ThrowsException() => _subject.Evaluate(string.Empty, "*a*");

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Evaluate_ActualValueIsEmptyString_ThrowsException() => _subject.Evaluate("Name", string.Empty);

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Evaluate_ExpectedValueIsNotString_ThrowsException() => _subject.Evaluate(1, "*a*");

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Evaluate_ActualValueIsNotString_ThrowsException() => _subject.Evaluate("Name", 1);

        [TestMethod]
        public void Evaluate_ExpectedValueMatchesActual_ReturnsTrue_Sample1()
        {
            AssertMatches("John Peters", "*ers*");
        }

        [TestMethod]
        public void Evaluate_ExpectedValueMatchesActual_ReturnsTrue_Sample2()
        {
            AssertMatches("John Peters", "John*");
        }

        [TestMethod]
        public void Evaluate_ExpectedValueMatchesActual_ReturnsTrue_Sample3()
        {
            AssertMatches("John Peters", "*Peters");
        }

        [TestMethod]
        public void Evaluate_ExpectedValueMatchesActual_ReturnsTrue_Sample4()
        {
            AssertMatches("John Peters", "John Peters");
        }

        [TestMethod]
        public void Evaluate_ExpectedValueDoesNotMatchesActual_ReturnsFalse_Sample1()
        {
            AssertMatches("John Peters", "John Peter", false);
        }

        [TestMethod]
        public void Evaluate_ExpectedValueDoesNotMatchesActual_ReturnsFalse_Sample2()
        {
            AssertMatches("John Peters", "*Pet", false);
        }

        [TestMethod]
        public void ToRql_ReturnsCorrectMatchesStatement()
        {
            // When
            var statement = _subject.ToRql("Name", "'*Peter*'");

            // Then
            Assert.AreEqual("search(Name, '*Peter*')", statement);
        }

        // Given a value and it's expected pattern to match
        private void AssertMatches(string value, string pattern, bool assertMatch = true)
        {
            // When evaluated
            bool matches = _subject.Evaluate(value, pattern);

            // Then it should return true
            if (assertMatch) Assert.IsTrue(matches);
            else Assert.IsFalse(matches);
        }
    }
}