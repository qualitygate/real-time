using QualityGate.RealTime.Domain;

namespace QualityGate.RealTime.Tests
{
    /// <summary>
    ///     A dummy entity to use in generic database tests.
    /// </summary>
    public record TestEntity : IEntity
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }
    }
}