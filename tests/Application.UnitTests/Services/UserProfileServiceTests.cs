using Application.Abstractions;
using Application.DTOs;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Application.UnitTests.Services;

public class UserProfileServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly UserProfileService _sut;

    public UserProfileServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        
        _sut = new UserProfileService(_dbContext, _httpContextAccessorMock.Object, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldReturnUserProfile_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@example.com",
            FullName = "Test User",
            PasswordHash = "hash"
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetCurrentAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.UserName.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldUpdateProfile_WhenValidRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var user = new User
        {
            Id = userId,
            UserName = "olduser",
            Email = "old@example.com",
            FullName = "Old Name",
            PasswordHash = "hash"
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateProfileRequest
        {
            UserName = "newuser",
            Email = "new@example.com",
            FullName = "New Name"
        };

        // Act
        var result = await _sut.UpdateProfileAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserName.Should().Be("newuser");
        result.Email.Should().Be("new@example.com");
        result.FullName.Should().Be("New Name");

        var userInDb = await _dbContext.Users.FindAsync(userId);
        userInDb!.UserName.Should().Be("newuser");
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldThrow_WhenUsernameTaken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var user = new User
        {
            Id = userId,
            UserName = "user1",
            Email = "user1@example.com",
            FullName = "User One",
            PasswordHash = "hash"
        };

        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "existinguser",
            Email = "other@example.com",
            FullName = "Other User",
            PasswordHash = "hash"
        };

        await _dbContext.Users.AddRangeAsync(user, otherUser);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateProfileRequest
        {
            UserName = "existinguser", // Taken
            Email = "user1@example.com"
        };

        // Act
        Func<Task> act = async () => await _sut.UpdateProfileAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("This username is already taken.");
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldThrow_WhenEmailTaken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var user = new User
        {
            Id = userId,
            UserName = "user1",
            Email = "user1@example.com",
            FullName = "User One",
            PasswordHash = "hash"
        };

        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "other",
            Email = "taken@example.com",
            FullName = "Other User",
            PasswordHash = "hash"
        };

        await _dbContext.Users.AddRangeAsync(user, otherUser);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateProfileRequest
        {
            UserName = "user1",
            Email = "taken@example.com" // Taken
        };

        // Act
        Func<Task> act = async () => await _sut.UpdateProfileAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("This email is already in use.");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldChangePassword_WhenCurrentPasswordCorrect()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var user = new User
        {
            Id = userId,
            UserName = "user",
            Email = "user@example.com",
            FullName = "User Name",
            PasswordHash = "old_hash"
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        _passwordHasherMock.Setup(x => x.VerifyPassword("old_password", "old_hash"))
            .Returns(true);
        _passwordHasherMock.Setup(x => x.HashPassword("new_password"))
            .Returns("new_hash");

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "old_password",
            NewPassword = "new_password"
        };

        // Act
        await _sut.ChangePasswordAsync(request, CancellationToken.None);

        // Assert
        var userInDb = await _dbContext.Users.FindAsync(userId);
        userInDb!.PasswordHash.Should().Be("new_hash");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrow_WhenCurrentPasswordIncorrect()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var user = new User
        {
            Id = userId,
            UserName = "user",
            Email = "user@example.com",
            FullName = "User Name",
            PasswordHash = "old_hash"
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        _passwordHasherMock.Setup(x => x.VerifyPassword("wrong_password", "old_hash"))
            .Returns(false);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "wrong_password",
            NewPassword = "new_password"
        };

        // Act
        Func<Task> act = async () => await _sut.ChangePasswordAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Current password is incorrect.");
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        // Act
        Func<Task> act = async () => await _sut.GetCurrentAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new UpdateProfileRequest
        {
            UserName = "newuser",
            Email = "new@example.com",
            FullName = "New Name"
        };

        // Act
        Func<Task> act = async () => await _sut.UpdateProfileAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "old_password",
            NewPassword = "new_password"
        };

        // Act
        Func<Task> act = async () => await _sut.ChangePasswordAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("User not found.");
    }

    // Helper method
    private void SetupAuthenticatedUser(Guid userId)
    {
        var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())
        }));
        
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }
}
