using Application.Abstractions;
using Application.DTOs.Auth;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Application.UnitTests.Services;

public class AuthServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly UserValidationHelpers _validationHelpers; // Real implementation instead of mock
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _tokenServiceMock = new Mock<ITokenService>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _validationHelpers = new UserValidationHelpers(_dbContext); // Real implementation

        _sut = new AuthService(_dbContext, _tokenServiceMock.Object, _passwordHasherMock.Object, _validationHelpers);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUserAndReturnTokens_WhenValidRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            UserName = "testuser",
            Password = "SecurePass123!",
            FullName = "Test User"
        };

        // No mock setup needed - using real UserValidationHelpers

        _passwordHasherMock.Setup(x => x.HashPassword(request.Password))
            .Returns("hashed_password");

        var expiresAt = DateTime.UtcNow.AddHours(1);
        var refreshExpires = DateTime.UtcNow.AddDays(7);
        
        _tokenServiceMock.Setup(x => x.CreateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
            .Returns(("access_token_123", expiresAt));
        _tokenServiceMock.Setup(x => x.CreateRefreshToken())
            .Returns(("refresh_token_456", refreshExpires));

        // Seed default User role
        var userRole = new Role { Id = Guid.NewGuid(), Name = "User" };
        await _dbContext.Roles.AddAsync(userRole);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token_123");
        result.RefreshToken.Should().Be("refresh_token_456");
        result.ExpiresAtUtc.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));

        var userInDb = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        userInDb.Should().NotBeNull();
        userInDb!.UserName.Should().Be("testuser");
        userInDb.FullName.Should().Be("Test User");
        userInDb.PasswordHash.Should().Be("hashed_password");

        var userRoleInDb = await _dbContext.UserRoles.AnyAsync(ur => ur.UserId == userInDb.Id && ur.RoleId == userRole.Id);
        userRoleInDb.Should().BeTrue();

        var refreshTokenInDb = await _dbContext.RefreshTokens.AnyAsync(rt => rt.Token == "refresh_token_456");
        refreshTokenInDb.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowInvalidOperation_WhenEmailAlreadyExists()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            UserName = "newuser",
            Password = "SecurePass123!",
            FullName = "Test User"
        };

        // Create existing user to trigger validation error
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            UserName = "existinguser",
            PasswordHash = "hash",
            FullName = "Existing User"
        };
        await _dbContext.Users.AddAsync(existingUser);
        await _dbContext.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email 'existing@example.com' is already in use.");
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowInvalidOperation_WhenUsernameAlreadyExists()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newemail@example.com",
            UserName = "existinguser",
            Password = "SecurePass123!",
            FullName = "Test User"
        };

        // Create existing user with same username to trigger validation error
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "other@example.com",
            UserName = "existinguser",
            PasswordHash = "hash",
            FullName = "Existing User"
        };
        await _dbContext.Users.AddAsync(existingUser);
        await _dbContext.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _sut.RegisterAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("UserName 'existinguser' is already taken.");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_WhenValidCredentials()
    {
        // Arrange
        var userRole = new Role { Id = Guid.NewGuid(), Name = "User" };
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "login@example.com",
            UserName = "loginuser",
            PasswordHash = "hashed_password",
            FullName = "Login User",
            UserRoles = new List<UserRole>
            {
                new UserRole { UserId = userId, RoleId = userRole.Id, Role = userRole }
            }
        };

        await _dbContext.Roles.AddAsync(userRole);
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var request = new LoginRequest
        {
            UserNameOrEmail = "loginuser",
            Password = "correct_password"
        };

        _passwordHasherMock.Setup(x => x.VerifyPassword("correct_password", "hashed_password"))
            .Returns(true);

        var expiresAt = DateTime.UtcNow.AddHours(1);
        var refreshExpires = DateTime.UtcNow.AddDays(7);
        
        _tokenServiceMock.Setup(x => x.CreateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
            .Returns(("access_token_login", expiresAt));
        _tokenServiceMock.Setup(x => x.CreateRefreshToken())
            .Returns(("refresh_token_login", refreshExpires));

        // Act
        var result = await _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token_login");
        result.RefreshToken.Should().Be("refresh_token_login");

        var refreshTokenInDb = await _dbContext.RefreshTokens.AnyAsync(rt => rt.Token == "refresh_token_login" && rt.UserId == userId);
        refreshTokenInDb.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowInvalidOperation_WhenPasswordIncorrect()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            UserName = "testuser",
            PasswordHash = "hashed_password",
            FullName = "Test User"
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var request = new LoginRequest
        {
            UserNameOrEmail = "testuser",
            Password = "wrong_password"
        };

        _passwordHasherMock.Setup(x => x.VerifyPassword("wrong_password", "hashed_password"))
            .Returns(false);

        // Act
        Func<Task> act = async () => await _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowInvalidOperation_WhenUserNotFound()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserNameOrEmail = "nonexistent@example.com",
            Password = "any_password"
        };

        // Act
        Func<Task> act = async () => await _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task RefreshAsync_ShouldReturnNewTokens_WhenValidRefreshToken()
    {
        // Arrange
        var userRole = new Role { Id = Guid.NewGuid(), Name = "User" };
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "refresh@example.com",
            UserName = "refreshuser",
            PasswordHash = "hash",
            FullName = "Refresh User",
            UserRoles = new List<UserRole>
            {
                new UserRole { UserId = userId, RoleId = userRole.Id, Role = userRole }
            }
        };

        var oldRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = "old_refresh_token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            User = user
        };

        await _dbContext.Roles.AddAsync(userRole);
        await _dbContext.Users.AddAsync(user);
        await _dbContext.RefreshTokens.AddAsync(oldRefreshToken);
        await _dbContext.SaveChangesAsync();

        var newExpiresAt = DateTime.UtcNow.AddHours(1);
        var newRefreshExpires = DateTime.UtcNow.AddDays(7);
        
        _tokenServiceMock.Setup(x => x.CreateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
            .Returns(("new_access_token", newExpiresAt));
        _tokenServiceMock.Setup(x => x.CreateRefreshToken())
            .Returns(("new_refresh_token", newRefreshExpires));

        // Act
        var result = await _sut.RefreshAsync("old_refresh_token", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");

        var oldTokenExists = await _dbContext.RefreshTokens.AnyAsync(rt => rt.Token == "old_refresh_token");
        oldTokenExists.Should().BeFalse("old token should be removed");

        var newTokenExists = await _dbContext.RefreshTokens.AnyAsync(rt => rt.Token == "new_refresh_token");
        newTokenExists.Should().BeTrue("new token should be created");
    }

    [Fact]
    public async Task RefreshAsync_ShouldThrowInvalidOperation_WhenRefreshTokenExpired()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "expired@example.com",
            UserName = "expireduser",
            PasswordHash = "hash",
            FullName = "Expired User"
        };

        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "expired_token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            User = user
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.RefreshTokens.AddAsync(expiredToken);
        await _dbContext.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _sut.RefreshAsync("expired_token", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid or expired refresh token.");
    }

    [Fact]
    public async Task LogoutAsync_ShouldRemoveRefreshToken_WhenValidToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "logout@example.com",
            UserName = "logoutuser",
            PasswordHash = "hash",
            FullName = "Logout User"
        };

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token_to_logout",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.RefreshTokens.AddAsync(refreshToken);
        await _dbContext.SaveChangesAsync();

        var request = new LogoutRequestDto { RefreshToken = "token_to_logout" };

        // Act
        await _sut.LogoutAsync(request, CancellationToken.None);

        // Assert
        var tokenExists = await _dbContext.RefreshTokens.AnyAsync(rt => rt.Token == "token_to_logout");
        tokenExists.Should().BeFalse("token should be removed after logout");
    }
}
