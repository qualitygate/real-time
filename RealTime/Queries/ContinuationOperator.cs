namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     Represents the base contract of RQL logical query operators used to chain query conditions, specifically:
    ///     AND and OR logical operators.
    /// </summary>
    /// <param name="Operator">String representing the actual RQL logical operators to tie conditions together.</param>
    public record ContinuationOperator(string Operator)
    {
        /// <summary>
        ///     RQL And logical operator.
        /// </summary>
        public static readonly ContinuationOperator And = new("and");

        /// <summary>
        ///     RQL Or logical operator.
        /// </summary>
        public static readonly ContinuationOperator Or = new("or");


        /// <summary>
        ///     Implicitly converts a string representation of a RQL logical operator to its
        ///     <see cref="ContinuationOperator"/> equivalent.
        /// </summary>
        /// <param name="operator">String representation of the RQL logical operator to convert.</param>
        /// <returns>The converter RQL logical operator in <see cref="ContinuationOperator"/> format.</returns>
        public static implicit operator ContinuationOperator(string @operator) => new(@operator);
    }
}