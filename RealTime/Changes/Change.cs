using System.Diagnostics.CodeAnalysis;

namespace QualityGate.RealTime.Changes
{
    /// <summary>
    ///     Represents a transformation that an Entity experienced in the database.
    /// </summary>
    public record Change(object Entity, string Table, ChangeType Type)
    {
        /// <summary>
        ///     Gets or sets the entity that is experiencing a change. In case of an entity deletion, here should be
        ///     an empty entity with the deleted one's ID.
        /// </summary>
        public object Entity { get; } = Entity;

        /// <summary>
        ///     Gets or sets the name of the Table where the changed entity resides.
        /// </summary>
        public string Table { get; } = Table;

        /// <summary>
        ///     Gets or sets the type of change the entity experienced.
        /// </summary>
        public ChangeType Type { get; [ExcludeFromCodeCoverage] init; } = Type;
    }
}