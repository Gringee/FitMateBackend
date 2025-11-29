using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs;
using Application.DTOs.Auth;
using FluentAssertions;
using WebApi.IntegrationTests.Common;
using Xunit;

namespace WebApi.IntegrationTests.Controllers;

public class UsersControllerSearchTests : BaseIntegrationTest
{
    public UsersControllerSearchTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Search_ShouldReturnUsers_WhenAuthenticatedAsRegularUser()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync("regular_user");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create another user to find
        await RegisterAndGetTokenAsync("target_user", "Target User");

        // Act
        var response = await Client.GetAsync("/api/users/search?filter=Target");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<UserSummaryDto>>();
        
        result.Should().NotBeNull();
        result.Should().Contain(u => u.FullName == "Target User");
    }

    [Fact]
    public async Task Search_ShouldReturnEmptyList_WhenNoMatch()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync("regular_user_2");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/users/search?filter=NonExistentUser123");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<UserSummaryDto>>();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ShouldReturn401_WhenNotAuthenticated()
    {
        // Act
        var response = await Client.GetAsync("/api/users/search?filter=test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ShouldReturn403_WhenAuthenticatedAsRegularUser()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync("regular_user_3");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<string> RegisterAndGetTokenAsync(string usernamePrefix, string fullName = "Test User")
    {
        var registerRequest = new RegisterRequest
        {
            Email = $"{usernamePrefix}_{Guid.NewGuid()}@example.com",
            UserName = $"{usernamePrefix}_{Guid.NewGuid().ToString().Substring(0, 8)}",
            Password = "SecurePass123!",
            FullName = fullName
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.AccessToken;
    }
}
