using Application.Abstractions;
using Application.DTOs;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Application.UnitTests.Services;

public class WorkoutSessionServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly WorkoutSessionService _sut;

    public WorkoutSessionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        
        _sut = new WorkoutSessionService(_dbContext, _httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task StartAsync_ShouldCreateSession_FromScheduledWorkout()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Test Plan",
            Type = "PPL",
            CreatedByUserId = userId
        };

        var scheduled = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            UserId = userId,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Test Plan",
            Plan = plan
        };

        var exercise = new ScheduledExercise
        {
            Id = Guid.NewGuid(),
            ScheduledWorkoutId = scheduled.Id,
            Name = "Bench Press",
            RestSeconds = 120
        };

        exercise.Sets.Add(new ScheduledSet
        {
            Id = Guid.NewGuid(),
            ScheduledExerciseId = exercise.Id,
            SetNumber = 1,
            Reps = 8,
            Weight = 100
        });

        scheduled.Exercises.Add(exercise);

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();

        var request = new StartSessionRequest { ScheduledId = scheduled.Id };

        // Act
        var result = await _sut.StartAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ScheduledId.Should().Be(scheduled.Id);
        result.Status.Should().Be("inprogress");
        result.Exercises.Should().HaveCount(1);
        result.Exercises.First().Name.Should().Be("Bench Press");
        result.Exercises.First().Sets.Should().HaveCount(1);

        var sessionInDb = await _dbContext.WorkoutSessions
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(s => s.Id == result.Id);
        sessionInDb.Should().NotBeNull();
        sessionInDb!.Status.Should().Be(WorkoutSessionStatus.InProgress);
    }

    [Fact]
    public async Task StartAsync_ShouldThrow_WhenScheduledWorkoutBelongsToAnotherUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var otherUserId = Guid.NewGuid();
        var plan = new Plan { Id = Guid.NewGuid(), PlanName = "Plan", Type = "PPL", CreatedByUserId = otherUserId };
        var scheduled = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            UserId = otherUserId, // Belongs to another user
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Plan",
            Plan = plan
        };

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();

        var request = new StartSessionRequest { ScheduledId = scheduled.Id };

        // Act
        Func<Task> act = async () => await _sut.StartAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Scheduled workout not found"); // Should behave as not found for security
    }

    [Fact]
    public async Task StartAsync_ShouldThrow_WhenScheduledNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var request = new StartSessionRequest { ScheduledId = Guid.NewGuid() };

        // Act
        Func<Task> act = async () => await _sut.StartAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Scheduled workout not found");
    }

    [Fact]
    public async Task PatchSetAsync_ShouldUpdateSetData_WhenSessionInProgress()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (session, setId) = await SetupSessionInProgressAsync(userId);

        var patchRequest = new PatchSetRequest
        {
            RepsDone = 10,
            WeightDone = 105,
            Rpe = 8,
            IsFailure = false
        };

        // Act
        var result = await _sut.PatchSetAsync(session.Id, setId, patchRequest, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var updatedSet = result.Exercises.First().Sets.First();
        updatedSet.RepsDone.Should().Be(10);
        updatedSet.WeightDone.Should().Be(105);
        updatedSet.Rpe.Should().Be(8);
        updatedSet.IsFailure.Should().BeFalse();
    }

    [Fact]
    public async Task PatchSetAsync_ShouldThrow_WhenSessionNotInProgress()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (session, setId) = await SetupSessionInProgressAsync(userId);

        // Complete the session
        session.Status = WorkoutSessionStatus.Completed;
        await _dbContext.SaveChangesAsync();

        var patchRequest = new PatchSetRequest { RepsDone = 10 };

        // Act
        Func<Task> act = async () => await _sut.PatchSetAsync(session.Id, setId, patchRequest, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Session not in progress");
    }

    [Fact]
    public async Task AddExerciseAsync_ShouldAddAdHocExercise_WhenSessionInProgress()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (session, _) = await SetupSessionInProgressAsync(userId);

        var request = new AddSessionExerciseRequest
        {
            Name = "Extra Curls",
            RestSecPlanned = 60,
            Sets = new List<AddSessionSetRequest>
            {
                new AddSessionSetRequest { RepsPlanned = 12, WeightPlanned = 20 }
            }
        };

        // Act
        var result = await _sut.AddExerciseAsync(session.Id, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Exercises.Should().HaveCount(2);
        var adHocExercise = result.Exercises.Last();
        adHocExercise.Name.Should().Be("Extra Curls");
        adHocExercise.Sets.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddExerciseAsync_ShouldThrow_WhenSessionNotInProgress()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (session, _) = await SetupSessionInProgressAsync(userId);

        // Complete the session
        session.Status = WorkoutSessionStatus.Completed;
        await _dbContext.SaveChangesAsync();

        var request = new AddSessionExerciseRequest
        {
            Name = "Extra Exercise",
            RestSecPlanned = 60,
            Sets = new List<AddSessionSetRequest>()
        };

        // Act
        Func<Task> act = async () => await _sut.AddExerciseAsync(session.Id, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot add exercises to a non in-progress session.");
    }

    [Fact]
    public async Task CompleteAsync_ShouldMarkAsCompleted_AndCalculateDuration()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (session, _) = await SetupSessionInProgressAsync(userId);

        var completedAt = session.StartedAtUtc.AddMinutes(45);
        var request = new CompleteSessionRequest
        {
            CompletedAtUtc = completedAt,
            SessionNotes = "Great workout!"
        };

        // Act
        var result = await _sut.CompleteAsync(session.Id, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("completed");
        result.DurationSec.Should().Be(45 * 60); // 45 minutes
        result.SessionNotes.Should().Be("Great workout!");

        var sessionInDb = await _dbContext.WorkoutSessions.FindAsync(session.Id);
        sessionInDb!.Status.Should().Be(WorkoutSessionStatus.Completed);
    }

    [Fact]
    public async Task CompleteAsync_ShouldUpdateScheduledStatus_ToPpleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (session, _) = await SetupSessionInProgressAsync(userId);

        var scheduled = await _dbContext.ScheduledWorkouts.FindAsync(session.ScheduledId);
        scheduled!.Status.Should().Be(ScheduledStatus.Planned);

        var request = new CompleteSessionRequest();

        // Act
        await _sut.CompleteAsync(session.Id, request, CancellationToken.None);

        // Assert
        var updatedScheduled = await _dbContext.ScheduledWorkouts.FindAsync(session.ScheduledId);
        updatedScheduled!.Status.Should().Be(ScheduledStatus.Completed);
    }

    [Fact]
    public async Task AbortAsync_ShouldMarkAsAborted_WithReason()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (session, _) = await SetupSessionInProgressAsync(userId);

        var request = new AbortSessionRequest
        {
            Reason = "Injury"
        };

        // Act
        var result = await _sut.AbortAsync(session.Id, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("aborted");
        result.SessionNotes.Should().Contain("Aborted: Injury");

        var sessionInDb = await _dbContext.WorkoutSessions.FindAsync(session.Id);
        sessionInDb!.Status.Should().Be(WorkoutSessionStatus.Aborted);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnSession_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (session, _) = await SetupSessionInProgressAsync(userId);

        // Act
        var result = await _sut.GetByIdAsync(session.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(session.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByRangeAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        // Create session within range
        var (sessionInRange, _) = await SetupSessionInProgressAsync(userId, DateTime.UtcNow.AddDays(-2));

        // Create session outside range
        await SetupSessionInProgressAsync(userId, DateTime.UtcNow.AddDays(-10));

        var fromUtc = DateTime.UtcNow.AddDays(-5);
        var toUtc = DateTime.UtcNow;

        // Act
        var result = await _sut.GetByRangeAsync(fromUtc, toUtc, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(sessionInRange.Id);
    }

    // Helper methods
    private void SetupAuthenticatedUser(Guid userId)
    {
        var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())
        }));
        
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    private async Task<(WorkoutSession session, Guid setId)> SetupSessionInProgressAsync(Guid userId, DateTime? startedAt = null)
    {
        SetupAuthenticatedUser(userId);

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Test Plan",
            Type = "PPL",
            CreatedByUserId = userId
        };

        var scheduled = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            UserId = userId,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Test Plan",
            Plan = plan
        };

        var exercise = new SessionExercise
        {
            Id = Guid.NewGuid(),
            WorkoutSessionId = Guid.Empty,
            Order = 1,
            Name = "Bench Press",
            RestSecPlanned = 120,
            IsAdHoc = false
        };

        var set = new SessionSet
        {
            Id = Guid.NewGuid(),
            SessionExerciseId = exercise.Id,
            SetNumber = 1,
            RepsPlanned = 8,
            WeightPlanned = 100
        };

        exercise.Sets.Add(set);

        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            ScheduledId = scheduled.Id,
            UserId = userId,
            StartedAtUtc = startedAt ?? DateTime.UtcNow,
            Status = WorkoutSessionStatus.InProgress
        };

        session.Exercises.Add(exercise);
        exercise.WorkoutSessionId = session.Id;

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.WorkoutSessions.AddAsync(session);
        await _dbContext.SaveChangesAsync();

        return (session, set.Id);
    }
    [Fact]
    public async Task PatchSetAsync_ShouldThrow_WhenSetNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (session, _) = await SetupSessionInProgressAsync(userId);

        var patchRequest = new PatchSetRequest { RepsDone = 10 };

        // Act
        Func<Task> act = async () => await _sut.PatchSetAsync(session.Id, Guid.NewGuid(), patchRequest, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Set not found in this session");
    }

    [Fact]
    public async Task CompleteAsync_ShouldThrow_WhenSessionNotInProgress()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (session, _) = await SetupSessionInProgressAsync(userId);

        // Complete the session
        session.Status = WorkoutSessionStatus.Completed;
        await _dbContext.SaveChangesAsync();

        var request = new CompleteSessionRequest();

        // Act
        Func<Task> act = async () => await _sut.CompleteAsync(session.Id, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Session not in progress");
    }

    [Fact]
    public async Task AbortAsync_ShouldThrow_WhenSessionNotInProgress()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (session, _) = await SetupSessionInProgressAsync(userId);

        // Complete the session
        session.Status = WorkoutSessionStatus.Completed;
        await _dbContext.SaveChangesAsync();

        var request = new AbortSessionRequest { Reason = "Test" };

        // Act
        Func<Task> act = async () => await _sut.AbortAsync(session.Id, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Session not in progress");
    }
    [Fact]
    public async Task CreateCompletedSessionFromScheduledAsync_ShouldCreateSession_WhenValidRequest()
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
            Time = new TimeOnly(18, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Plan",
            Plan = plan
        };

        var exercise = new ScheduledExercise
        {
            Id = Guid.NewGuid(),
            ScheduledWorkoutId = scheduled.Id,
            Name = "Squat",
            RestSeconds = 180
        };
        exercise.Sets.Add(new ScheduledSet { Id = Guid.NewGuid(), ScheduledExerciseId = exercise.Id, SetNumber = 1, Reps = 5, Weight = 100 });
        scheduled.Exercises.Add(exercise);

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();

        var request = new CompleteScheduledRequest
        {
            StartedAtUtc = DateTime.UtcNow.AddHours(-1),
            CompletedAtUtc = DateTime.UtcNow,
            SessionNotes = "Done quickly",
            PopulateActuals = true
        };

        // Act
        var result = await _sut.CreateCompletedSessionFromScheduledAsync(scheduled.Id, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("completed");
        result.DurationSec.Should().NotBeNull();
        result.DurationSec!.Value.Should().BeCloseTo(3600, 5); // ~1 hour
        result.SessionNotes.Should().Be("Done quickly");

        var sessionInDb = await _dbContext.WorkoutSessions.Include(x => x.Exercises).ThenInclude(e => e.Sets).FirstOrDefaultAsync(x => x.Id == result.Id);
        sessionInDb!.Status.Should().Be(WorkoutSessionStatus.Completed);
        sessionInDb.Exercises.First().Sets.First().RepsDone.Should().Be(5); // Populated
        sessionInDb.Exercises.First().Sets.First().WeightDone.Should().Be(100); // Populated

        var scheduledInDb = await _dbContext.ScheduledWorkouts.FindAsync(scheduled.Id);
        scheduledInDb!.Status.Should().Be(ScheduledStatus.Completed);
    }

    [Fact]
    public async Task CreateCompletedSessionFromScheduledAsync_ShouldNotPopulateActuals_WhenFlagIsFalse()
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

        var exercise = new ScheduledExercise { Id = Guid.NewGuid(), ScheduledWorkoutId = scheduled.Id, Name = "Squat" };
        exercise.Sets.Add(new ScheduledSet { Id = Guid.NewGuid(), ScheduledExerciseId = exercise.Id, SetNumber = 1, Reps = 5, Weight = 100 });
        scheduled.Exercises.Add(exercise);

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();

        var request = new CompleteScheduledRequest { PopulateActuals = false };

        // Act
        var result = await _sut.CreateCompletedSessionFromScheduledAsync(scheduled.Id, request, CancellationToken.None);

        // Assert
        var sessionInDb = await _dbContext.WorkoutSessions.Include(x => x.Exercises).ThenInclude(e => e.Sets).FirstOrDefaultAsync(x => x.Id == result.Id);
        sessionInDb!.Exercises.First().Sets.First().RepsDone.Should().BeNull();
        sessionInDb.Exercises.First().Sets.First().WeightDone.Should().BeNull();
    }

    [Fact]
    public async Task CreateCompletedSessionFromScheduledAsync_ShouldThrowKeyNotFound_WhenScheduledWorkoutNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);
        var request = new CompleteScheduledRequest();

        // Act
        Func<Task> act = async () => await _sut.CreateCompletedSessionFromScheduledAsync(Guid.NewGuid(), request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Scheduled workout not found");
    }

    [Fact]
    public async Task CreateCompletedSessionFromScheduledAsync_ShouldThrowInvalidOperation_WhenAlreadyCompleted()
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
            Status = ScheduledStatus.Completed, // Already completed
            PlanName = "Plan",
            Plan = plan
        };

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();

        var request = new CompleteScheduledRequest();

        // Act
        Func<Task> act = async () => await _sut.CreateCompletedSessionFromScheduledAsync(scheduled.Id, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("This workout is already completed.");
    }

    [Fact]
    public async Task CreateCompletedSessionFromScheduledAsync_ShouldThrowInvalidOperation_WhenSessionAlreadyExists()
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
            Status = ScheduledStatus.Planned,
            PlanName = "Plan",
            Plan = plan
        };

        // Create an existing session linked to this scheduled workout
        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            ScheduledId = scheduled.Id,
            UserId = userId,
            Status = WorkoutSessionStatus.InProgress
        };

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.WorkoutSessions.AddAsync(session);
        await _dbContext.SaveChangesAsync();

        var request = new CompleteScheduledRequest();

        // Act
        Func<Task> act = async () => await _sut.CreateCompletedSessionFromScheduledAsync(scheduled.Id, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Active or completed session already exists for this scheduled workout.");
    }
}
