using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.UnitTests.Services;

public class UserValidationHelpersTests
{
    private readonly AppDbContext _dbContext;
    private readonly UserValidationHelpers _sut;

    public UserValidationHelpersTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _sut = new UserValidationHelpers(_dbContext);
    }

    [Fact]
    public async Task EnsureEmailIsUniqueAsync_ShouldThrow_WhenEmailExists()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User { Id = Guid.NewGuid(), Email = email, UserName = "user1", FullName = "User One", PasswordHash = "hash" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var act = async () => await _sut.EnsureEmailIsUniqueAsync(email, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Email '{email}' is already in use.");
    }

    [Fact]
    public async Task EnsureEmailIsUniqueAsync_ShouldThrow_WhenEmailExistsCaseInsensitive()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User { Id = Guid.NewGuid(), Email = email, UserName = "user1", FullName = "User One", PasswordHash = "hash" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var act = async () => await _sut.EnsureEmailIsUniqueAsync("TEST@example.com", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Email 'TEST@example.com' is already in use.");
    }

    [Fact]
    public async Task EnsureEmailIsUniqueAsync_ShouldNotThrow_WhenEmailIsUnique()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "existing@example.com", UserName = "user1", FullName = "User One", PasswordHash = "hash" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var act = async () => await _sut.EnsureEmailIsUniqueAsync("new@example.com", CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureEmailIsUniqueAsync_ShouldNotThrow_WhenEmailBelongsToExcludedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var user = new User { Id = userId, Email = email, UserName = "user1", FullName = "User One", PasswordHash = "hash" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var act = async () => await _sut.EnsureEmailIsUniqueAsync(email, CancellationToken.None, excludeUserId: userId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureUserNameIsUniqueAsync_ShouldThrow_WhenUserNameExists()
    {
        // Arrange
        var userName = "testuser";
        var user = new User { Id = Guid.NewGuid(), UserName = userName, Email = "test@example.com", FullName = "User One", PasswordHash = "hash" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var act = async () => await _sut.EnsureUserNameIsUniqueAsync(userName, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"UserName '{userName}' is already taken.");
    }

    [Fact]
    public async Task EnsureUserNameIsUniqueAsync_ShouldNotThrow_WhenUserNameIsUnique()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), UserName = "existinguser", Email = "test@example.com", FullName = "User One", PasswordHash = "hash" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var act = async () => await _sut.EnsureUserNameIsUniqueAsync("newuser", CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureUserNameIsUniqueAsync_ShouldNotThrow_WhenUserNameBelongsToExcludedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userName = "testuser";
        var user = new User { Id = userId, UserName = userName, Email = "test@example.com", FullName = "User One", PasswordHash = "hash" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var act = async () => await _sut.EnsureUserNameIsUniqueAsync(userName, CancellationToken.None, excludeUserId: userId);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
