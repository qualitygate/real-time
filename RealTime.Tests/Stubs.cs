using System;

namespace QualityGate.RealTime.Tests
{
    /// <summary>
    ///     Contains Stubbing helpers to simplify tests where these objects are created and used.
    /// </summary>
    public static class Stubs
    {
        public static TestEntity NewEntity => new()
        {
            Id = new Random().Next(100).ToString(),
            Name = "Jordan"
        };
    }
}