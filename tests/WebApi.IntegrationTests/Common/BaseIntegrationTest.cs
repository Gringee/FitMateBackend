using Xunit;

namespace WebApi.IntegrationTests.Common;

[Collection("IntegrationTests")]
public abstract class BaseIntegrationTest
{
    protected readonly HttpClient Client;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Client = factory.CreateClient();
    }
}
