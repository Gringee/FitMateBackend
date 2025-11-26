using Application.Abstractions;
using Application.DTOs.Analytics;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Globalization;

namespace Application.UnitTests.Services;

public class AnalyticsServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly AnalyticsService _sut;

    public AnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        
        _sut = new AnalyticsService(_dbContext, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task GetOverviewAsync_ShouldReturnCorrectVolumeAndIntensity()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var session = await CreateCompletedSessionAsync(userId, DateTime.UtcNow, 100, 10); // 1000 volume, 100 intensity

        // Act
        var result = await _sut.GetOverviewAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalVolume.Should().Be(1000);
        result.AvgWeight.Should().Be(100);
        result.SessionsCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOverviewAsync_ShouldReturnZero_WhenNoSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        // Act
        var result = await _sut.GetOverviewAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), CancellationToken.None);

        // Assert
        result.TotalVolume.Should().Be(0);
        result.AvgWeight.Should().Be(0);
        result.SessionsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetOverviewAsync_ShouldCalculateAdherenceCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        await CreateScheduledWorkoutAsync(userId, DateTime.Today, ScheduledStatus.Completed);
        await CreateScheduledWorkoutAsync(userId, DateTime.Today.AddDays(1), ScheduledStatus.Planned);

        // Act
        var result = await _sut.GetOverviewAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(2), CancellationToken.None);

        // Assert
        result.AdherencePct.Should().Be(50);
    }

    [Fact]
    public async Task GetVolumeAsync_ShouldGroupByDay()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        await CreateCompletedSessionAsync(userId, DateTime.UtcNow, 100, 10);

        // Act
        var result = await _sut.GetVolumeAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), "day", null, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Period.Should().Be(DateTime.UtcNow.ToString("yyyy-MM-dd"));
        result.First().Value.Should().Be(1000);
    }

    [Fact]
    public async Task GetVolumeAsync_ShouldGroupByWeek()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var date = DateTime.UtcNow;
        await CreateCompletedSessionAsync(userId, date, 100, 10);

        // Act
        var result = await _sut.GetVolumeAsync(date.AddDays(-1), date.AddDays(1), "week", null, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Period.Should().Contain("-W");
        result.First().Value.Should().Be(1000);
    }

    [Fact]
    public async Task GetVolumeAsync_ShouldGroupByExercise()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        await CreateCompletedSessionAsync(userId, DateTime.UtcNow, 100, 10, "Squat");
        await CreateCompletedSessionAsync(userId, DateTime.UtcNow, 50, 10, "Bench");

        // Act
        var result = await _sut.GetVolumeAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), "exercise", null, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(x => x.ExerciseName == "Squat" && x.Value == 1000);
        result.Should().Contain(x => x.ExerciseName == "Bench" && x.Value == 500);
    }

    [Fact]
    public async Task GetVolumeAsync_ShouldFilterByExerciseName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        await CreateCompletedSessionAsync(userId, DateTime.UtcNow, 100, 10, "Squat");
        await CreateCompletedSessionAsync(userId, DateTime.UtcNow, 50, 10, "Bench");

        // Act
        var result = await _sut.GetVolumeAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), "day", "Squat", CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Value.Should().Be(1000);
    }

    [Fact]
    public async Task GetE1RmAsync_ShouldCalculateE1RmCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        // E1RM = Weight * (1 + Reps/30)
        // 100 * (1 + 30/30) = 200
        await CreateCompletedSessionAsync(userId, DateTime.UtcNow, 100, 30, "Deadlift");

        // Act
        var result = await _sut.GetE1RmAsync("Deadlift", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().E1Rm.Should().Be(200);
    }

    [Fact]
    public async Task GetE1RmAsync_ShouldReturnEmpty_WhenNoData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        // Act
        var result = await _sut.GetE1RmAsync("Deadlift", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAdherenceAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        await CreateScheduledWorkoutAsync(userId, DateTime.Today, ScheduledStatus.Completed);
        await CreateScheduledWorkoutAsync(userId, DateTime.Today, ScheduledStatus.Planned);
        await CreateScheduledWorkoutAsync(userId, DateTime.Today, ScheduledStatus.Planned);

        // Act
        var result = await _sut.GetAdherenceAsync(DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today), CancellationToken.None);

        // Assert
        result.Planned.Should().Be(3);
        result.Completed.Should().Be(1);
    }

    [Fact]
    public async Task GetAdherenceAsync_ShouldHandleDateRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        await CreateScheduledWorkoutAsync(userId, DateTime.Today, ScheduledStatus.Completed);
        await CreateScheduledWorkoutAsync(userId, DateTime.Today.AddDays(5), ScheduledStatus.Completed); // Outside range

        // Act
        var result = await _sut.GetAdherenceAsync(DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today.AddDays(1)), CancellationToken.None);

        // Assert
        result.Planned.Should().Be(1);
        result.Completed.Should().Be(1);
    }

    [Fact]
    public async Task GetPlanVsActualAsync_ShouldReturnItems_WhenSessionExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var session = await CreateCompletedSessionAsync(userId, DateTime.UtcNow, 100, 10, "Squat");

        // Act
        var result = await _sut.GetPlanVsActualAsync(session.Id, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().ExerciseName.Should().Be("Squat");
        result.First().WeightDone.Should().Be(100);
        result.First().RepsDone.Should().Be(10);
    }

    [Fact]
    public async Task GetPlanVsActualAsync_ShouldReturnEmpty_WhenSessionNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        // Act
        var result = await _sut.GetPlanVsActualAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPlanVsActualAsync_ShouldIdentifyExtraSets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var session = await CreateCompletedSessionAsync(userId, DateTime.UtcNow, 100, 10, "Squat", isAdHoc: true);

        // Act
        var result = await _sut.GetPlanVsActualAsync(session.Id, CancellationToken.None);

        // Assert
        result.First().IsExtra.Should().BeTrue();
    }

    [Fact]
    public async Task GetOverviewAsync_ShouldHandleDateSwapping()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        await CreateCompletedSessionAsync(userId, DateTime.UtcNow, 100, 10);

        // Act - Pass To before From
        var result = await _sut.GetOverviewAsync(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(-1), CancellationToken.None);

        // Assert
        result.TotalVolume.Should().Be(1000);
    }

    // Helpers
    private void SetupAuthenticatedUser(Guid userId)
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
    }

    private async Task<WorkoutSession> CreateCompletedSessionAsync(Guid userId, DateTime date, decimal weight, int reps, string exerciseName = "Test Exercise", bool isAdHoc = false)
    {
        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartedAtUtc = date,
            CompletedAtUtc = date.AddHours(1),
            Status = WorkoutSessionStatus.Completed
        };

        var exercise = new SessionExercise
        {
            Id = Guid.NewGuid(),
            WorkoutSessionId = session.Id,
            Name = exerciseName,
            IsAdHoc = isAdHoc,
            Order = 1
        };

        var set = new SessionSet
        {
            Id = Guid.NewGuid(),
            SessionExerciseId = exercise.Id,
            SetNumber = 1,
            WeightDone = weight,
            RepsDone = reps,
            WeightPlanned = weight,
            RepsPlanned = reps
        };

        exercise.Sets.Add(set);
        session.Exercises.Add(exercise);

        await _dbContext.WorkoutSessions.AddAsync(session);
        await _dbContext.SaveChangesAsync();

        return session;
    }

    private async Task CreateScheduledWorkoutAsync(Guid userId, DateTime date, ScheduledStatus status)
    {
        var scheduled = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = DateOnly.FromDateTime(date),
            Time = new TimeOnly(10, 0),
            Status = status,
            PlanName = "Test Plan"
        };

        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();
    }
}
