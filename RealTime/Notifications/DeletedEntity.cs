using System.Diagnostics.CodeAnalysis;
using QualityGate.RealTime.Domain;

namespace QualityGate.RealTime.Notifications
{
    /// <summary>
    ///     Represents an entity that was deleted.
    /// </summary>
    /// <param name="Id">Identifier of the deleted entity.</param>
    public record DeletedEntity(string Id) : IEntity
    {
        /// <summary>
        ///     Gets or sets the identifier of the deleted entity.
        /// </summary>
        public string? Id { get; [ExcludeFromCodeCoverage] set; } = Id;
    }
}