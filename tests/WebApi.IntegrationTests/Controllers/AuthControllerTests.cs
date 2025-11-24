using System.Net;
using System.Net.Http.Json;
using Application.DTOs.Auth;
using FluentAssertions;
using WebApi.IntegrationTests.Common;
using Xunit;

namespace WebApi.IntegrationTests.Controllers;

public class AuthControllerTests : BaseIntegrationTest
{
    public AuthControllerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenRequestIsEmpty()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenRequestIsEmpty()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task Register_ShouldReturn200OK_AndCreateUser_WhenValidRequest()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "integration@example.com",
            UserName = "integrationuser",
            Password = "SecurePass123!",
            FullName = "Integration Test User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
        authResponse.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ShouldReturn200OK_AndReturnTokens_WhenValidCredentials()
    {
        // Arrange - First register a user
        var registerRequest = new RegisterRequest
        {
            Email = "logintest@example.com",
            UserName = "loginuser",
            Password = "SecurePass123!",
            FullName = "Login Test User"
        };
        await Client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            UserNameOrEmail = "loginuser",
            Password = "SecurePass123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Refresh_ShouldReturn200OK_AndReturnNewTokens_WhenValidRefreshToken()
    {
        // Arrange - Register and login to get a refresh token
        var registerRequest = new RegisterRequest
        {
            Email = "refreshtest@example.com",
            UserName = "refreshuser",
            Password = "SecurePass123!",
            FullName = "Refresh Test User"
        };
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var initialAuth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshRequest = new RefreshRequestDto
        {
            RefreshToken = initialAuth!.RefreshToken
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
        authResponse.AccessToken.Should().NotBe(initialAuth.AccessToken, "new access token should be different");
    }

    [Fact]
    public async Task Logout_ShouldReturn204NoContent_WhenAuthenticated()
    {
        // Arrange - Register and login to get tokens
        var registerRequest = new RegisterRequest
        {
            Email = "logouttest@example.com",
            UserName = "logoutuser",
            Password = "SecurePass123!",
            FullName = "Logout Test User"
        };
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Set Authorization header  
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        var logoutRequest = new LogoutRequestDto
        {
            RefreshToken = authResponse.RefreshToken
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/logout", logoutRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Register_ShouldReturn400BadRequest_WhenUsernameTaken()
    {
        // Arrange - Register first user
        var firstRequest = new RegisterRequest
        {
            Email = "first@example.com",
            UserName = "duplicateuser",
            Password = "SecurePass123!",
            FullName = "First User"
        };
        await Client.PostAsJsonAsync("/api/auth/register", firstRequest);

        // Try to register with same username
        var secondRequest = new RegisterRequest
        {
            Email = "second@example.com",
            UserName = "duplicateuser",
            Password = "SecurePass123!",
            FullName = "Second User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", secondRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ShouldReturn400BadRequest_WhenEmailTaken()
    {
        // Arrange - Register first user
        var firstRequest = new RegisterRequest
        {
            Email = "duplicate@example.com",
            UserName = "firstuser",
            Password = "SecurePass123!",
            FullName = "First User"
        };
        await Client.PostAsJsonAsync("/api/auth/register", firstRequest);

        // Try to register with same email
        var secondRequest = new RegisterRequest
        {
            Email = "duplicate@example.com",
            UserName = "seconduser",
            Password = "SecurePass123!",
            FullName = "Second User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", secondRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ShouldReturn400BadRequest_WhenPasswordTooShort()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            UserName = "testuser",
            Password = "Short1", // Too short
            FullName = "Test User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ShouldReturn400BadRequest_WhenUserNotFound()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserNameOrEmail = "nonexistent@example.com",
            Password = "AnyPassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ShouldReturn400BadRequest_WhenPasswordIncorrect()
    {
        // Arrange - Register a user first
        var registerRequest = new RegisterRequest
        {
            Email = "wrongpass@example.com",
            UserName = "wrongpassuser",
            Password = "CorrectPass123!",
            FullName = "Wrong Pass User"
        };
        await Client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Try to login with wrong password
        var loginRequest = new LoginRequest
        {
            UserNameOrEmail = "wrongpassuser",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_ShouldReturn400BadRequest_WhenTokenInvalid()
    {
        // Arrange
        var request = new RefreshRequestDto
        {
            RefreshToken = "invalid_token_that_does_not_exist"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
