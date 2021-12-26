using System;
using System.Text.RegularExpressions;

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
        ///     Determines given string matches a certain pattern.
        /// </summary>
        /// <param name="expected">String object to check whether it matches a pattern.</param>
        /// <param name="actual">String representing the pattern to check <see cref="expected"/> matches.</param>
        /// <exception cref="ArgumentException"><paramref name="expected"/> or <paramref name="actual"/> are not strings.</exception>
        /// <returns>
        ///     True if <paramref name="expected"/> matches the pattern defined on <paramref name="actual"/> argument.
        /// </returns>
        public override bool Evaluate(object? expected, object? actual)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));

            if (expected is not string expectedValue || string.IsNullOrEmpty(expectedValue))
                throw new ArgumentException("Must be non-empty string", nameof(expected));
            if (actual is not string actualValue || string.IsNullOrEmpty(actualValue))
                throw new ArgumentException("Must be non-empty string", nameof(actual));

            return Regex.IsMatch(expectedValue, actualValue);
        }

        /// <summary>
        ///     Gets the RQL statement of this operator.
        /// </summary>
        /// <param name="leftOperand">Field to apply the match to.</param>
        /// <param name="rightOperand">Pattern to apply in the match statement.</param>
        /// <returns>
        ///     A statement in the form (given leftOperand be "field1" and right operand: 'term'):
        ///     <code>"regex(field1, '^.*term.*$')"</code>
        /// </returns>
        public override string ToRql(string leftOperand, string rightOperand)
        {
            rightOperand = Regex.IsMatch(rightOperand, "^\'.*\'$") ? rightOperand[1..^1] : rightOperand;
            
            return $"regex({leftOperand}, '{rightOperand}')";
        }
    }
}