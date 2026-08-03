namespace ProductRequests.IntegrationTests;

public sealed class InfrastructureSmokeTests
{
    [Fact]
    public void ApiAssemblyLoads()
    {
        Assert.Equal("ProductRequests.Api", typeof(Program).Assembly.GetName().Name);
    }
}
