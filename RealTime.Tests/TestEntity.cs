using QualityGate.RealTime.Domain;

namespace QualityGate.RealTime.Tests
{
    /// <summary>
    ///     A dummy entity to use in generic database tests.
    /// </summary>
    public record TestEntity : IEntity
    {
        public string? Id { get; set; }

        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }
    }
}