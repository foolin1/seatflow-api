namespace SeatFlow.UnitTests;

public sealed class SolutionStructureTests
{
    [Fact]
    public void DomainAssembly_HasExpectedName()
    {
        var assemblyName = typeof(
                global::SeatFlow.Domain.AssemblyReference)
            .Assembly
            .GetName()
            .Name;

        Assert.Equal("SeatFlow.Domain", assemblyName);
    }
}