using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace WebApi.IntegrationTests.Common;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("fitmate_test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public IntegrationTestWebAppFactory()
    {
        Environment.SetEnvironmentVariable("JwtSettings__Secret", "super_secret_key_for_integration_tests_12345");
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "FitMateBackend");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "FitMateClient");
        Environment.SetEnvironmentVariable("JwtSettings__AccessTokenMinutes", "60");
        Environment.SetEnvironmentVariable("JwtSettings__RefreshTokenDays", "7");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebAppFactory>
{
}
