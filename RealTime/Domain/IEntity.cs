namespace QualityGate.RealTime.Domain
{
    /// <summary>
    ///     Base contact of all domain entities.
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        ///     Gets or sets the identifier of the current entity.
        /// </summary>
        string? Id { get; set; }
    }
}