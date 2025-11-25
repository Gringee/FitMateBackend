using Application.DTOs;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Application.UnitTests.Services;

public class WorkoutSessionServiceCoverageTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly WorkoutSessionService _sut;

    public WorkoutSessionServiceCoverageTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _sut = new WorkoutSessionService(_dbContext, _httpContextAccessorMock.Object);
    }

    private void SetupAuthenticatedUser(Guid userId)
    {
        var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())
        }));
        
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    [Fact]
    public async Task CreateCompletedSessionFromScheduledAsync_ShouldClampCompletedAt_WhenBeforeStartedAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var plan = new Plan { Id = Guid.NewGuid(), PlanName = "Plan", Type = "PPL", CreatedByUserId = userId };
        var scheduled = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            UserId = userId,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Status = ScheduledStatus.Planned,
            PlanName = "Plan",
            Plan = plan
        };

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var request = new CompleteScheduledRequest
        {
            StartedAtUtc = now,
            CompletedAtUtc = now.AddMinutes(-10) // Before start!
        };

        // Act
        var result = await _sut.CreateCompletedSessionFromScheduledAsync(scheduled.Id, request, CancellationToken.None);

        // Assert
        result.CompletedAtUtc.Should().Be(now); // Should be clamped to startedAt
        result.DurationSec.Should().Be(0);
    }

    [Fact]
    public async Task CompleteAsync_ShouldHandleLocalAndUnspecifiedDates()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartedAtUtc = DateTime.UtcNow.AddHours(-1),
            Status = WorkoutSessionStatus.InProgress,
            Scheduled = new ScheduledWorkout { Id = Guid.NewGuid(), PlanName = "P", UserId = userId, Plan = new Plan { Id = Guid.NewGuid(), PlanName = "P", Type = "T", CreatedByUserId = userId } }
        };
        _dbContext.WorkoutSessions.Add(session);
        await _dbContext.SaveChangesAsync();

        var localDate = DateTime.Now; // Local
        var unspecifiedDate = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

        // Act 1: Local
        var req1 = new CompleteSessionRequest { CompletedAtUtc = localDate };
        await _sut.CompleteAsync(session.Id, req1, CancellationToken.None);
        var s1 = await _dbContext.WorkoutSessions.FindAsync(session.Id);
        s1!.CompletedAtUtc.Value.Kind.Should().Be(DateTimeKind.Utc);
        
        // Reset
        s1.Status = WorkoutSessionStatus.InProgress;
        await _dbContext.SaveChangesAsync();

        // Act 2: Unspecified
        var req2 = new CompleteSessionRequest { CompletedAtUtc = unspecifiedDate };
        await _sut.CompleteAsync(session.Id, req2, CancellationToken.None);
        var s2 = await _dbContext.WorkoutSessions.FindAsync(session.Id);
        s2!.CompletedAtUtc.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task AbortAsync_ShouldAppendReason_WhenNotesExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartedAtUtc = DateTime.UtcNow,
            Status = WorkoutSessionStatus.InProgress,
            SessionNotes = "Existing notes",
            Scheduled = new ScheduledWorkout { Id = Guid.NewGuid(), PlanName = "P", UserId = userId, Plan = new Plan { Id = Guid.NewGuid(), PlanName = "P", Type = "T", CreatedByUserId = userId } }
        };
        _dbContext.WorkoutSessions.Add(session);
        await _dbContext.SaveChangesAsync();

        var request = new AbortSessionRequest { Reason = "Too tired" };

        // Act
        var result = await _sut.AbortAsync(session.Id, request, CancellationToken.None);

        // Assert
        result.SessionNotes.Should().Be("Existing notes\nAborted: Too tired");
    }

    [Fact]
    public async Task AbortAsync_ShouldNotChangeNotes_WhenReasonEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartedAtUtc = DateTime.UtcNow,
            Status = WorkoutSessionStatus.InProgress,
            SessionNotes = "Existing notes",
            Scheduled = new ScheduledWorkout { Id = Guid.NewGuid(), PlanName = "P", UserId = userId, Plan = new Plan { Id = Guid.NewGuid(), PlanName = "P", Type = "T", CreatedByUserId = userId } }
        };
        _dbContext.WorkoutSessions.Add(session);
        await _dbContext.SaveChangesAsync();

        var request = new AbortSessionRequest { Reason = "" };

        // Act
        var result = await _sut.AbortAsync(session.Id, request, CancellationToken.None);

        // Assert
        result.SessionNotes.Should().Be("Existing notes");
    }

    [Fact]
    public async Task AddExerciseAsync_ShouldHandleFirstExercise_WhenNoExercisesExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartedAtUtc = DateTime.UtcNow,
            Status = WorkoutSessionStatus.InProgress,
            Scheduled = new ScheduledWorkout { Id = Guid.NewGuid(), PlanName = "P", UserId = userId, Plan = new Plan { Id = Guid.NewGuid(), PlanName = "P", Type = "T", CreatedByUserId = userId } }
        };
        _dbContext.WorkoutSessions.Add(session);
        await _dbContext.SaveChangesAsync();

        var request = new AddSessionExerciseRequest
        {
            Name = "First Ex",
            Sets = new List<AddSessionSetRequest> { new() { RepsPlanned = 10, WeightPlanned = 50 } }
        };

        // Act
        var result = await _sut.AddExerciseAsync(session.Id, request, CancellationToken.None);

        // Assert
        result.Exercises.Should().HaveCount(1);
        result.Exercises.First().Order.Should().Be(1);
    }
}
