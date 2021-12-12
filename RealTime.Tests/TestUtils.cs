using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QualityGate.RealTime.Tests
{
    public static class TestUtils
    {
        private const int OperationTimeout = 5000;

        /// <summary>
        ///     Compares to two objects that are assumed that the first object's properties are all present in the second
        ///     object. It checks value by value in the properties of the first object and compares each value with the
        ///     value of the same named property in the second object.
        /// </summary>
        /// <param name="first">An object to compare its properties with the other one.</param>
        /// <param name="second">The object which properties will be compared with the first passed one.</param>
        /// <param name="excludeProperties">
        ///     Array of properties names setting which ones to exclude in the comparison.
        /// </param>
        /// <exception cref="AssertFailedException">
        ///     Thrown when found a property in first that is not present in second, or when any of the values in a property
        ///     in first is not equal to its corresponding one in the second object.
        /// </exception>
        public static void AssertEqualByValues<T>(this T first, T second, params string[] excludeProperties)
        {
            Assert.IsNotNull(first);
            Assert.IsNotNull(second);

            var properties =
                from property in first.GetType().GetProperties()
                where !excludeProperties.Contains(property.Name)
                select property;

            foreach (var property in properties)
            {
                var propertyInSecond = (from p in second.GetType().GetProperties()
                    where !excludeProperties.Contains(property.Name) && p.Name == property.Name
                    select p).Single();

                var valueInFirst = TryGetProperty(property, first, "actual");
                var valueInSecond = TryGetProperty(propertyInSecond, second, "expected");

                Assert.AreEqual(valueInSecond, valueInFirst, $"Property: '{property.Name}' was not equal in both " +
                                                             "objects.\n" +
                                                             $"Actual value: {valueInFirst}\n" +
                                                             $"Expected value: {valueInSecond}\n");
            }
        }

        /// <summary>
        /// Waits an amount of time for the task to complete.
        /// </summary>
        /// <param name="task">A <see cref="Task"/> to wait for its completion.</param>
        /// <param name="timeout">Milliseconds to wait for the task to complete.</param>
        /// <exception cref="AssertFailedException">Thrown when the task time out.</exception>
        public static void WaitFor(this Task task, int timeout = OperationTimeout)
        {
            if (!task.Wait(timeout))
            {
                Assert.Fail($"Task timed out after: {timeout} milliseconds");
            }
        }

        /// <summary>
        /// Waits an amount of time for the task to complete and get its result.
        /// </summary>
        /// <param name="task">A <see cref="Task{T}"/> to wait for its completion and get its result.</param>
        /// <param name="timeout">Milliseconds to wait for the task to complete.</param>
        /// <typeparam name="T">Type of the expected result yielded by the task.</typeparam>
        /// <returns>An instance of <typeparamref name="T"/> if the task finished before the timeout.</returns>
        /// <exception cref="AssertFailedException">Thrown when the task time out.</exception>
        public static T WaitForResult<T>(this Task<T> task, int timeout = OperationTimeout)
        {
            if (task.Wait(timeout))
            {
                return task.Result;
            }

            Assert.Fail($"Task timed out after: {timeout} milliseconds");
            return default;
        }

        private static object TryGetProperty<T>(PropertyInfo property, T @object, string role)
        {
            try
            {
                return property.GetValue(@object);
            }
            catch (ArgumentException)
            {
                Assert.Fail($"Property: {property.Name} was not present in object: {@object} which was the: {role}");
                return null;
            }
        }
    }
}