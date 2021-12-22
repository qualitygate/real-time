using QualityGate.RealTime.Domain;

namespace QualityGate.RealTime.Tests.Domain
{
    public record AnotherEntity : IEntity
    {
        public string? Id { get; set; }

        public int Age { get; set; }
    }
}