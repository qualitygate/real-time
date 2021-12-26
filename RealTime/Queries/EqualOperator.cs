namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     Operator representing the = sign. Compares two objects checking its equality.
    /// </summary>
    public class EqualOperator : Operator
    {
        /// <summary>
        ///     Returns the "=" operator symbol.
        /// </summary>
        public override string Symbol => "=";

        /// <summary>
        ///     Compares by value the given two objects and checks their equality.
        /// </summary>
        /// <param name="expected">Object to compare the second <paramref name="actual"/> value with.</param>
        /// <param name="actual">Object to check its equality to <see cref="expected"/> one.</param>
        /// <returns>
        ///     True if both objects are null, or (represented as string) they are not null and equal; false in
        ///     any other case.
        /// </returns>
        public override bool Evaluate(object? expected, object? actual)
        {
            var bothAreNull = expected is null && actual is null;

            return bothAreNull || expected?.ToString()?.Equals(actual?.ToString()) == true;
        }

        /// <summary>
        ///     Gets the RQL statement of this operator.
        /// </summary>
        /// <param name="leftOperand">Left operand.</param>
        /// <param name="rightOperand">Right operand.</param>
        /// <returns>
        ///     A statement in the form (given leftOperand be "field1" and right operand: 2):
        ///     <code>"field1 = 2"</code>
        /// </returns>
        public override string ToRql(string leftOperand, string rightOperand) => $"{leftOperand} = {rightOperand}";
    }
}