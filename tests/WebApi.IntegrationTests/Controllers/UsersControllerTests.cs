using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs;
using Application.DTOs.Auth;
using FluentAssertions;
using WebApi.IntegrationTests.Common;
using Xunit;

namespace WebApi.IntegrationTests.Controllers;

public class UsersControllerTests : BaseIntegrationTest
{
    public UsersControllerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await Client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await Client.PostAsync("/api/users", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ShouldReturn403Forbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_ShouldReturn200OK_WhenUserIsAdmin()
    {
        // Arrange
        var adminToken = await GetAdminTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await Client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNull();
        users!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAll_ShouldFilterUsers_WhenSearchProvided()
    {
        // Arrange
        var adminToken = await GetAdminTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await Client.GetAsync("/api/users?search=admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNull();
        users!.Should().Contain(u => u.UserName.Contains("admin"));
    }

    [Fact]
    public async Task Create_ShouldReturn201Created_WhenUserIsAdmin()
    {
        // Arrange
        var adminToken = await GetAdminTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createDto = new CreateUserDto
        {
            UserName = $"newuser_{Guid.NewGuid().ToString().Substring(0, 8)}",
            Email = $"newuser_{Guid.NewGuid()}@test.com",
            FullName = "New User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/users", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.UserName.Should().Be(createDto.UserName);
    }

    [Fact]
    public async Task Create_ShouldReturn400BadRequest_WhenUsernameTaken()
    {
        // Arrange
        var adminToken = await GetAdminTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // First, create a user
        var username = $"duplicateuser_{Guid.NewGuid().ToString().Substring(0, 8)}";
        var firstDto = new CreateUserDto
        {
            UserName = username,
            Email = $"first_{Guid.NewGuid()}@test.com",
            FullName = "First User"
        };
        await Client.PostAsJsonAsync("/api/users", firstDto);

        // Try to create another with same username
        var secondDto = new CreateUserDto
        {
            UserName = username,
            Email = $"second_{Guid.NewGuid()}@test.com",
            FullName = "Second User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/users", secondDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ShouldReturn204NoContent_WhenUserIsAdmin()
    {
        // Arrange
        var adminToken = await GetAdminTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Create a user first
        var createDto = new CreateUserDto
        {
            UserName = $"updateme_{Guid.NewGuid().ToString().Substring(0, 8)}",
            Email = $"updateme_{Guid.NewGuid()}@test.com",
            FullName = "Update Me"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/users", createDto);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // Update the user
        var updateDto = new UpdateUserDto
        {
            UserName = $"updated_{Guid.NewGuid().ToString().Substring(0, 8)}",
            Email = $"updated_{Guid.NewGuid()}@test.com",
            FullName = "Updated Name"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/users/{createdUser!.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_ShouldReturn404NotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var adminToken = await GetAdminTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var updateDto = new UpdateUserDto { UserName = "newname" };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/users/{Guid.NewGuid()}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignRole_ShouldReturn204NoContent_WhenValid()
    {
        // Arrange
        var adminToken = await GetAdminTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Create a user
        var createDto = new CreateUserDto
        {
            UserName = $"roleuser_{Guid.NewGuid().ToString().Substring(0, 8)}",
            Email = $"roleuser_{Guid.NewGuid()}@test.com",
            FullName = "Role User"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/users", createDto);
        var user = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // Act
        var response = await Client.PutAsync($"/api/users/{user!.Id}/role?roleName=User", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AssignRole_ShouldReturn400BadRequest_WhenRoleDoesNotExist()
    {
        // Arrange
        var adminToken = await GetAdminTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createDto = new CreateUserDto
        {
            UserName = $"user_{Guid.NewGuid().ToString().Substring(0, 8)}",
            Email = $"user_{Guid.NewGuid()}@test.com",
            FullName = "User"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/users", createDto);
        var user = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // Act
        var response = await Client.PutAsync($"/api/users/{user!.Id}/role?roleName=NonExistentRole", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssignRole_ShouldReturn409Conflict_WhenUserAlreadyHasRole()
    {
        // Arrange
        var adminToken = await GetAdminTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createDto = new CreateUserDto
        {
            UserName = $"user_{Guid.NewGuid().ToString().Substring(0, 8)}",
            Email = $"user_{Guid.NewGuid()}@test.com",
            FullName = "User"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/users", createDto);
        var user = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // Assign role first time
        await Client.PutAsync($"/api/users/{user!.Id}/role?roleName=User", null);

        // Act - Try to assign again
        var response = await Client.PutAsync($"/api/users/{user.Id}/role?roleName=User", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_ShouldReturn204NoContent_WhenUserIsNotAdmin()
    {
        // Arrange
        var adminToken = await GetAdminTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createDto = new CreateUserDto
        {
            UserName = $"deleteme_{Guid.NewGuid().ToString().Substring(0, 8)}",
            Email = $"deleteme_{Guid.NewGuid()}@test.com",
            FullName = "Delete Me"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/users", createDto);
        var user = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // Act
        var response = await Client.DeleteAsync($"/api/users/{user!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ShouldReturn404NotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var adminToken = await GetAdminTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await Client.DeleteAsync($"/api/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn204NoContent_WhenUserExists()
    {
        // Arrange
        var adminToken = await GetAdminTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createDto = new CreateUserDto
        {
            UserName = $"pwdreset_{Guid.NewGuid().ToString().Substring(0, 8)}",
            Email = $"pwdreset_{Guid.NewGuid()}@test.com",
            FullName = "Pwd Reset"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/users", createDto);
        var user = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        var resetDto = new ResetPasswordDto { NewPassword = "NewSecurePass123!" };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/users/{user!.Id}/reset-password", resetDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn404NotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var adminToken = await GetAdminTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var resetDto = new ResetPasswordDto { NewPassword = "NewSecurePass123!" };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/users/{Guid.NewGuid()}/reset-password", resetDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Helper methods
    private async Task<string> RegisterAndGetTokenAsync()
    {
        var registerRequest = new RegisterRequest
        {
            Email = $"testuser_{Guid.NewGuid()}@example.com",
            UserName = $"testuser_{Guid.NewGuid().ToString().Substring(0, 8)}",
            Password = "SecurePass123!",
            FullName = "Test User"
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.AccessToken;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        // Use default seeded admin credentials
        var loginRequest = new LoginRequest
        {
            UserNameOrEmail = "admin",
            Password = "Admin123!"
        };

        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get admin token: {response.StatusCode}");
        }

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.AccessToken;
    }
}
