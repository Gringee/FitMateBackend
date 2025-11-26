using Application.Abstractions;
using Application.DTOs.BodyMetrics;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Application.UnitTests.Services;

public class BodyMetricsServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IFriendshipService> _friendsMock;
    private readonly BodyMetricsService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public BodyMetricsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"BodyMetrics_Test_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _currentUserMock = new Mock<ICurrentUserService>();
        _currentUserMock.Setup(x => x.UserId).Returns(_userId);
        
        _friendsMock = new Mock<IFriendshipService>();

        _service = new BodyMetricsService(_db, _currentUserMock.Object, _friendsMock.Object);
    }

    [Fact]
    public async Task AddMeasurementAsync_ShouldCreateMeasurement_WithCorrectBMI()
    {
        // Arrange
        var dto = new CreateBodyMeasurementDto(
            WeightKg: 70,
            HeightCm: 175,
            BodyFatPercentage: 15.5m,
            ChestCm: 95,
            WaistCm: 80,
            HipsCm: null,
            BicepsCm: null,
            ThighsCm: null,
            Notes: "Test measurement"
        );

        // Act
        var result = await _service.AddMeasurementAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.WeightKg.Should().Be(70);
        result.HeightCm.Should().Be(175);
        
        // BMI = 70 / (1.75 * 1.75) = 22.86
        result.BMI.Should().BeApproximately(22.86m, 0.01m);
        result.BodyFatPercentage.Should().Be(15.5m);
        result.ChestCm.Should().Be(95);
        result.WaistCm.Should().Be(80);
        result.Notes.Should().Be("Test measurement");

        // Verify in database
        var dbMeasurement = await _db.BodyMeasurements.FirstOrDefaultAsync(m => m.Id == result.Id);
        dbMeasurement.Should().NotBeNull();
        dbMeasurement!.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task GetMeasurementsAsync_ShouldReturnAllMeasurements_WhenNoFilters()
    {
        // Arrange
        await SeedMeasurementsAsync();

        // Act
        var result = await _service.GetMeasurementsAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInDescendingOrder(m => m.MeasuredAtUtc);
    }

    [Fact]
    public async Task GetMeasurementsAsync_ShouldFilterByDateRange()
    {
        // Arrange
        await SeedMeasurementsAsync();
        var from = DateTime.UtcNow.AddDays(-15);
        var to = DateTime.UtcNow.AddDays(-5);

        // Act
        var result = await _service.GetMeasurementsAsync(from, to);

        // Assert
        result.Should().HaveCount(1);
        result.First().WeightKg.Should().Be(72);
    }

    [Fact]
    public async Task GetLatestMeasurementAsync_ShouldReturnMostRecent()
    {
        // Arrange
        await SeedMeasurementsAsync();

        // Act
        var result = await _service.GetLatestMeasurementAsync();

        // Assert
        result.Should().NotBeNull();
        result!.WeightKg.Should().Be(75); // Most recent
    }

    [Fact]
    public async Task GetLatestMeasurementAsync_ShouldReturnNull_WhenNoMeasurements()
    {
        // Act
        var result = await _service.GetLatestMeasurementAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStatsAsync_ShouldCalculateCorrectStats()
    {
        // Arrange
        await SeedMeasurementsAsync();

        // Act
        var result = await _service.GetStatsAsync();

        // Assert
        result.Should().NotBeNull();
        result.CurrentWeightKg.Should().Be(75);
        result.CurrentBMI.Should().BeApproximately(24.49m, 0.01m);
        result.BMICategory.Should().Be("Normal");
        result.LowestWeight.Should().Be(70);
        result.HighestWeight.Should().Be(75);
        result.TotalMeasurements.Should().Be(3);
        
        // Weight change: 75 (latest) - 70 (30+ days ago) = 5
        result.WeightChangeLast30Days.Should().Be(5);
    }

    [Fact]
    public async Task GetStatsAsync_ShouldReturnNulls_WhenNoMeasurements()
    {
        // Act
        var result = await _service.GetStatsAsync();

        // Assert
        result.CurrentWeightKg.Should().BeNull();
        result.CurrentBMI.Should().BeNull();
        result.BMICategory.Should().BeNull();
        result.WeightChangeLast30Days.Should().BeNull();
        result.LowestWeight.Should().BeNull();
        result.HighestWeight.Should().BeNull();
        result.TotalMeasurements.Should().Be(0);
    }

    [Fact]
    public async Task GetProgressAsync_ShouldReturnFilteredAndOrderedMeasurements()
    {
        // Arrange
        await SeedMeasurementsAsync();
        var from = DateTime.UtcNow.AddDays(-40);
        var to = DateTime.UtcNow.AddDays(-5);

        // Act
        var result = await _service.GetProgressAsync(from, to);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(m => m.Date);
        result.First().WeightKg.Should().Be(70);
        result.Last().WeightKg.Should().Be(72);
    }

    [Fact]
    public async Task DeleteMeasurementAsync_ShouldRemoveMeasurement()
    {
        // Arrange
        var measurement = new BodyMeasurement
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            MeasuredAtUtc = DateTime.UtcNow,
            WeightKg = 70,
            HeightCm = 175,
            BMI = 22.86m
        };
        _db.BodyMeasurements.Add(measurement);
        await _db.SaveChangesAsync();

        // Act
        await _service.DeleteMeasurementAsync(measurement.Id);

        // Assert
        var deleted = await _db.BodyMeasurements.FirstOrDefaultAsync(m => m.Id == measurement.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteMeasurementAsync_ShouldThrow_WhenMeasurementNotFound()
    {
        // Act
        Func<Task> act = async () => await _service.DeleteMeasurementAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Measurement not found");
    }

    [Fact]
    public async Task DeleteMeasurementAsync_ShouldThrow_WhenMeasurementBelongsToAnotherUser()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var measurement = new BodyMeasurement
        {
            Id = Guid.NewGuid(),
            UserId = otherUserId,
            MeasuredAtUtc = DateTime.UtcNow,
            WeightKg = 70,
            HeightCm = 175,
            BMI = 22.86m
        };
        _db.BodyMeasurements.Add(measurement);
        await _db.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.DeleteMeasurementAsync(measurement.Id);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetFriendMetricsAsync_ShouldReturnMetrics_WhenFriendsAndSharingEnabled()
    {
        // Arrange
        var friendId = Guid.NewGuid();
        
        // Mock friendship
        _friendsMock.Setup(x => x.AreFriendsAsync(_userId, friendId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Create friend user with sharing enabled
        var friend = new User
        {
            Id = friendId,
            UserName = "friend",
            FullName = "Friend User",
            Email = "friend@test.com",
            PasswordHash = "hash",
            ShareBiometricsWithFriends = true
        };
        _db.Users.Add(friend);

        // Add metrics for friend
        var measurement = new BodyMeasurement
        {
            Id = Guid.NewGuid(),
            UserId = friendId,
            MeasuredAtUtc = DateTime.UtcNow,
            WeightKg = 80,
            HeightCm = 180,
            BMI = 24.69m
        };
        _db.BodyMeasurements.Add(measurement);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetFriendMetricsAsync(friendId);

        // Assert
        result.Should().HaveCount(1);
        result.First().WeightKg.Should().Be(80);
    }

    [Fact]
    public async Task GetFriendMetricsAsync_ShouldThrow_WhenNotFriends()
    {
        // Arrange
        var friendId = Guid.NewGuid();
        _friendsMock.Setup(x => x.AreFriendsAsync(_userId, friendId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _service.GetFriendMetricsAsync(friendId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("User is not your friend.");
    }

    [Fact]
    public async Task GetFriendMetricsAsync_ShouldReturnEmpty_WhenSharingDisabled()
    {
        // Arrange
        var friendId = Guid.NewGuid();
        
        _friendsMock.Setup(x => x.AreFriendsAsync(_userId, friendId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var friend = new User
        {
            Id = friendId,
            UserName = "friend",
            FullName = "Friend User",
            Email = "friend@test.com",
            PasswordHash = "hash",
            ShareBiometricsWithFriends = false // Disabled
        };
        _db.Users.Add(friend);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetFriendMetricsAsync(friendId);

        // Assert
        result.Should().BeEmpty();
    }

    private async Task SeedMeasurementsAsync()
    {
        var measurements = new List<BodyMeasurement>
        {
            new BodyMeasurement
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                MeasuredAtUtc = DateTime.UtcNow.AddDays(-35),
                WeightKg = 70,
                HeightCm = 175,
                BMI = 22.86m
            },
            new BodyMeasurement
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                MeasuredAtUtc = DateTime.UtcNow.AddDays(-10),
                WeightKg = 72,
                HeightCm = 175,
                BMI = 23.51m
            },
            new BodyMeasurement
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                MeasuredAtUtc = DateTime.UtcNow,
                WeightKg = 75,
                HeightCm = 175,
                BMI = 24.49m
            }
        };

        _db.BodyMeasurements.AddRange(measurements);
        await _db.SaveChangesAsync();
    }
}
