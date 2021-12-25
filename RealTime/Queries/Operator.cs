using System;

namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     Base class of all the logical operators to use in the queries.
    /// </summary>
    public abstract class Operator
    {
        /// <summary>
        ///     Equality logical operator.
        /// </summary>
        public static readonly Operator Equal = new EqualOperator();
        public static readonly Operator NotEqual = new NotEqualOperator();
        public static readonly Operator Matches = new MatchesOperator();


        /// <summary>
        ///     Implicitly converts a logical operator in string representation into its <see cref="Operator"/>
        ///     corresponding instance.
        /// </summary>
        /// <param name="operator">String representation of the operator to convert.</param>
        /// <returns>
        ///     The <see cref="Operator"/> corresponding instance to the given <paramref name="operator"/>.
        /// </returns>
        public static implicit operator Operator(string @operator) => @operator switch
        {
            "=" => new EqualOperator(),
            "<>" => new NotEqualOperator(),
            "matches" => new MatchesOperator(),
            _ => throw new NotImplementedException()
        };

        public static implicit operator string(Operator @operator) => @operator.Symbol;


        /// <summary>
        ///     When overridden in a derived class the actual sign of this operator.
        /// </summary>
        public abstract string Symbol { get; }


        /// <summary>
        ///     When overridden in a derived class it evaluates both operands of this operator.
        /// </summary>
        /// <param name="expected">First operand to participate in the operator.</param>
        /// <param name="actual">Second operand to participate in the operator.</param>
        /// <returns>True if both operands satisfy this operator; false otherwise.</returns>
        public abstract bool Evaluate(object? expected, object? actual);
    }
}