using QualityGate.RealTime.Domain;

namespace QualityGate.RealTime.Tests.Domain
{
    public record AnotherEntity : IEntity
    {
        public string? Id { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int Age { get; set; }
    }
}