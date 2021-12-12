namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     Operator representing the = sign. Compares two objects checking its equality.
    /// </summary>
    public class EqualOperator : OperatorBase
    {
        public override string Sign => "=";

        /// <summary>
        ///     Compares by value the given two objects.
        /// </summary>
        /// <param name="expected">Object to compare the second <paramref name="actual"/> value with.</param>
        /// <param name="actual">Object to check its equality to <see cref="expected"/> one.</param>
        /// <returns>True if both objects are null, or are not null and equal; false in any other case.</returns>
        public override bool Evaluate(object? expected, object? actual)
        {
            var bothAreNull = expected is null && actual is null;

            return bothAreNull || expected?.ToString()?.Equals(actual?.ToString()) == true;
        }
    }
}