using System.Diagnostics.CodeAnalysis;

namespace QualityGate.RealTime.Changes
{
    /// <summary>
    ///     This record contains the information a changed entity in the format as client browsers it to be.
    /// </summary>
    /// <remarks>
    ///     The internal reference of the Entity is of object type, to allow the JSON serializer properly
    ///     introspect it, and not be biased by a certain type, to which reference there is no access at compile
    ///     time.
    /// </remarks>
    // ReSharper disable UnusedAutoPropertyAccessor.Local
    // ReSharper disable MemberCanBePrivate.Local
    public record ExternalChange(object Entity, ChangeType Type)
    {
        /// <summary>
        ///     Gets the entity that changed.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        [ExcludeFromCodeCoverage]
        // ReSharper disable once UnusedMember.Global
        public object Entity { get; } = Entity;

        /// <summary>
        ///     Gets the type of change the entity experienced.
        /// </summary>
        [ExcludeFromCodeCoverage]
        // ReSharper disable once UnusedMember.Global
        public ChangeType Type { get; } = Type;


        public static ExternalChange FromChange(Change change) => new(change.Entity, change.Type);
    }
}