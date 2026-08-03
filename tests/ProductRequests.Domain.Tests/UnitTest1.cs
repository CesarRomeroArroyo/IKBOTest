using ProductRequests.Domain;

namespace ProductRequests.Domain.Tests;

public sealed class ArchitectureSmokeTests
{
    [Fact]
    public void DomainAssemblyLoads()
    {
        Assert.Equal("ProductRequests.Domain", typeof(AssemblyReference).Assembly.GetName().Name);
    }
}
