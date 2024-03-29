using System.Text;
using QualityGate.RealTime.Changes;
using QualityGate.RealTime.Utils;

namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     Represents a condition to evaluate whether a domain entity satisfy a query.
    /// </summary>
    public record Condition
    {
        /// <summary>
        ///     Initializes a new instance of <see cref="Condition"/> class with a field name, logical operator and
        ///     value to compare with. 
        /// </summary>
        /// <param name="field">Name of the domain entity's field to evaluate.</param>
        /// <param name="operator">Logical operator to use in the field evaluation.</param>
        /// <param name="value">
        ///     The value that in order for the condition to be true, must be compared to the domain entity's field value
        ///     using the specified logical operator.
        /// </param>
        public Condition(string field, Operator @operator, object? value = null)
        {
            Field = field;
            Operator = @operator;
            Value = value?.ParseValue();
        }


        /// <summary>
        ///     Implicitly converts a <see cref="ConditionDto"/> object into a <see cref="Condition"/> one.
        /// </summary>
        /// <param name="conditionDto">DTO to copy the data from and place in the resultant condition.</param>
        /// <returns>
        ///     The resultant condition that haves all the values gotten from <paramref name="conditionDto"/>.
        /// </returns>
        public static implicit operator Condition(ConditionDto conditionDto)
        {
            var condition = new Condition(conditionDto.Field, conditionDto.Operator, conditionDto.Value);

            if (conditionDto.JoinUsing is not null)  condition.JoinUsing = conditionDto.JoinUsing;
            
            condition.LeftParenthesis = conditionDto.LeftParenthesis;
            condition.RightParenthesis = conditionDto.RightParenthesis;

            return condition;
        }

        /// <summary>
        ///     Gets or sets the logical operator to use in order to chain this condition with the next in the query.
        /// </summary>
        public JoinOperator? JoinUsing { get; set; }

        /// <summary>
        ///     Gets the name of the domain entity field to involve in the condition evaluation.
        /// </summary>
        public string Field { get; }

        /// <summary>
        ///     Gets or sets an optional value saying whether or not this condition has a parenthesis to its left.
        /// </summary>
        public bool? LeftParenthesis { get; set; }

        /// <summary>
        ///     Gets the logical operator to use in the comparison.
        /// </summary>
        public Operator Operator { get; }

        /// <summary>
        ///     Gets or sets an optional value saying whether or not this condition has a parenthesis to its right.
        /// </summary>
        public bool? RightParenthesis { get; set; }

        /// <summary>
        ///     Gets the value to compare the domain entity field with.
        /// </summary>
        public object? Value { get; }


        /// <summary>
        ///     Evaluates whether the provided entity in the given change matches this query conditions.
        /// </summary>
        /// <param name="change">
        ///     A change that occurred in the domain entity, where the entity reference resides.
        /// </param>
        /// <returns>True if the changed domain entity still satisfies this query.</returns>
        public bool Match(Change change)
        {
            var (entity, _, _) = change;
            var propertyInfo = entity.GetType().GetProperty(Field);
            var propertyValue = propertyInfo?.GetValue(entity);

            return Operator.Equal.Evaluate(Value, propertyValue);
        }

        /// <summary>
        ///     Gets the RQL representation of this condition.
        /// </summary>
        /// <returns>The RQL representation of this condition.</returns>
        public string ToRql()
        {
            var builder = new StringBuilder();

            var value = Value switch
            {
                string stringValue => $"'{stringValue}'",
                null => "null",
                _ => $"{Value}"
            };
            var leftParenthesis = LeftParenthesis ?? false ? "(" : string.Empty;
            var rightParenthesis = RightParenthesis ?? false ? ")" : string.Empty;
            var statement = $"{leftParenthesis}{Operator.ToRql(Field, value)}{rightParenthesis}";
            builder.Append(statement);

            if (JoinUsing is not null) builder.Append($" {JoinUsing.Operator}");

            return builder.ToString();
        }
    }
}