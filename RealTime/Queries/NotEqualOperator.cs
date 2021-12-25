namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     Operator representing the &lt;&gt; sign. Compares two objects checking its inequality.
    /// </summary>
    public class NotEqualOperator : Operator
    {
        /// <summary>
        ///     Returns the "&lt;&gt;" operator symbol.
        /// </summary>
        public override string Symbol => "<>";

        /// <summary>
        ///     Compares by value the given two objects.
        /// </summary>
        /// <param name="expected">Object to compare the second <paramref name="actual"/> value with.</param>
        /// <param name="actual">Object to check its equality to <see cref="expected"/> one.</param>
        /// <returns>
        ///     True both given values (represented as strings) are not equal; false otherwise.
        /// </returns>
        public override bool Evaluate(object? expected, object? actual)
        {
            // ReSharper disable once ArrangeRedundantParentheses
            return (expected is null && actual is not null) ||
                   expected is not null && expected.ToString()?.Equals(actual?.ToString()) == false;
        }
    }
}