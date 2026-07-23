namespace SeatFlow.IntegrationTests;

public sealed class ApiAssemblyTests
{
    [Fact]
    public void ApiAssembly_HasExpectedName()
    {
        var assemblyName = typeof(global::Program)
            .Assembly
            .GetName()
            .Name;

        Assert.Equal("SeatFlow.Api", assemblyName);
    }
}