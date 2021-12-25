namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     DTOs that contains the specification of a query condition.
    /// </summary>
    /// <param name="Field">Domain entity Field name whose value will be compared in the condition.</param>
    /// <param name="Operator">The logical operator to apply in the comparison.</param>
    /// <param name="JoinUsing">
    ///     Optional continuation logical operator to chain this condition with the next in the query.
    /// </param>
    /// <param name="LeftParenthesis">
    ///     If defined, tells whether or not the current condition presents a parenthesis to its left.
    /// </param>
    /// <param name="RightParenthesis">
    ///     If defined, tells whether or not the current condition presents a parenthesis to its right.
    /// </param>
    /// <param name="Value">The value to compare the domain entity field value with.</param>
    public record ConditionDto(
        string Field,
        string Operator,
        string? JoinUsing = null,
        bool? LeftParenthesis = null,
        bool? RightParenthesis = null,
        object? Value = null);
}