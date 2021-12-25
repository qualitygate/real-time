using System;

namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     Represents the "Matches" operator. The one that signal that the condition where it's present should check
    ///     whether the field value matches the expression defined in the condition's value.
    /// </summary>
    public class MatchesOperator : Operator
    {
        /// <summary>
        ///     Returns the "matches" operator symbol.
        /// </summary>
        public override string Symbol => "matches";

        
        /// <summary>
        ///     Compares by value the given two objects.
        /// </summary>
        /// <param name="expected">Object to compare the second <paramref name="actual"/> value with.</param>
        /// <param name="actual">Object to check its equality to <see cref="expected"/> one.</param>
        /// <returns>
        ///     True if both objects are null, or (represented as string) they are not null and equal; false in
        ///     any other case.
        /// </returns>
        public override bool Evaluate(object? expected, object? actual)
        {
            throw new NotImplementedException();
        }
    }
}