using Application.Abstractions;
using Application.DTOs;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.ComponentModel.DataAnnotations;

namespace Application.UnitTests.Services;

public class UserProfileTargetWeightTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly UserProfileService _sut;

    public UserProfileTargetWeightTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _currentUserMock = new Mock<ICurrentUserService>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        
        _sut = new UserProfileService(_dbContext, _currentUserMock.Object, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task GetTargetWeightAsync_ShouldReturnTargetWeight()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            FullName = "Test User",
            PasswordHash = "hash",
            TargetWeightKg = 75.5m
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetTargetWeightAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TargetWeightKg.Should().Be(75.5m);
    }

    [Fact]
    public async Task GetTargetWeightAsync_ShouldReturnNull_WhenNotSet()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            FullName = "Test User",
            PasswordHash = "hash",
            TargetWeightKg = null
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetTargetWeightAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TargetWeightKg.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTargetWeightAsync_ShouldSetTargetWeight()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            FullName = "Test User",
            PasswordHash = "hash",
            TargetWeightKg = null
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateTargetWeightRequest { TargetWeightKg = 80.0m };

        // Act
        await _sut.UpdateTargetWeightAsync(request, CancellationToken.None);

        // Assert
        var updatedUser = await _dbContext.Users.FindAsync(userId);
        updatedUser!.TargetWeightKg.Should().Be(80.0m);
    }

    [Fact]
    public async Task UpdateTargetWeightAsync_WithNull_ShouldClearTargetWeight()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            FullName = "Test User",
            PasswordHash = "hash",
            TargetWeightKg = 75.0m
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateTargetWeightRequest { TargetWeightKg = null };

        // Act
        await _sut.UpdateTargetWeightAsync(request, CancellationToken.None);

        // Assert
        var updatedUser = await _dbContext.Users.FindAsync(userId);
        updatedUser!.TargetWeightKg.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTargetWeightAsync_WithZero_ShouldClearTargetWeight()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            FullName = "Test User",
            PasswordHash = "hash",
            TargetWeightKg = 75.0m
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateTargetWeightRequest { TargetWeightKg = 0 };

        // Act
        await _sut.UpdateTargetWeightAsync(request, CancellationToken.None);

        // Assert
        var updatedUser = await _dbContext.Users.FindAsync(userId);
        updatedUser!.TargetWeightKg.Should().BeNull();
    }

    [Fact]
    public async Task UpdateBiometricsPrivacyAsync_ShouldEnableSharing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            FullName = "Test User",
            PasswordHash = "hash",
            ShareBiometricsWithFriends = false
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        await _sut.UpdateBiometricsPrivacyAsync(true, CancellationToken.None);

        // Assert
        var updatedUser = await _dbContext.Users.FindAsync(userId);
        updatedUser!.ShareBiometricsWithFriends.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBiometricsPrivacyAsync_ShouldDisableSharing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            FullName = "Test User",
            PasswordHash = "hash",
            ShareBiometricsWithFriends = true
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        await _sut.UpdateBiometricsPrivacyAsync(false, CancellationToken.None);

        // Assert
        var updatedUser = await _dbContext.Users.FindAsync(userId);
        updatedUser!.ShareBiometricsWithFriends.Should().BeFalse();
    }

    [Fact]
    public void UpdateTargetWeightRequest_Validation_ShouldRejectBelowRange()
    {
        // Arrange
        var request = new UpdateTargetWeightRequest { TargetWeightKg = 39.9m };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("40-200 kg");
    }

    [Fact]
    public void UpdateTargetWeightRequest_Validation_ShouldRejectAboveRange()
    {
        // Arrange
        var request = new UpdateTargetWeightRequest { TargetWeightKg = 200.1m };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("40-200 kg");
    }

    [Fact]
    public void UpdateTargetWeightRequest_Validation_ShouldAcceptZero()
    {
        // Arrange
        var request = new UpdateTargetWeightRequest { TargetWeightKg = 0 };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateTargetWeightRequest_Validation_ShouldAcceptValidRange()
    {
        // Arrange
        var request = new UpdateTargetWeightRequest { TargetWeightKg = 75.5m };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeTrue();
    }

    // Helper method
    private void SetupAuthenticatedUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }
}
