using System;

namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     Base class of all the logical operators to use in the queries.
    /// </summary>
    public abstract class OperatorBase
    {
        /// <summary>
        ///     Equality logical operator.
        /// </summary>
        public static readonly OperatorBase Equal = new EqualOperator();
        public static readonly OperatorBase NotEqual = new NotEqualOperator();


        /// <summary>
        ///     Implicitly converts a logical operator in string representation into its <see cref="OperatorBase"/>
        ///     corresponding instance.
        /// </summary>
        /// <param name="operator">String representation of the operator to convert.</param>
        /// <returns>
        ///     The <see cref="OperatorBase"/> corresponding instance to the given <paramref name="operator"/>.
        /// </returns>
        public static implicit operator OperatorBase(string @operator) => @operator switch
        {
            "=" => new EqualOperator(),
            "<>" => new NotEqualOperator(),
            _ => throw new NotImplementedException()
        };


        /// <summary>
        ///     When overridden in a derived class the actual sign of this operator.
        /// </summary>
        public abstract string Sign { get; }


        /// <summary>
        ///     When overridden in a derived class it evaluates both operands of this operator.
        /// </summary>
        /// <param name="expected">First operand to participate in the operator.</param>
        /// <param name="actual">Second operand to participate in the operator.</param>
        /// <returns>True if both operands satisfy this operator; false otherwise.</returns>
        public abstract bool Evaluate(object? expected, object? actual);
    }
}