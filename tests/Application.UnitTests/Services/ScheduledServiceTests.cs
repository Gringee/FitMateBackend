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

public class ScheduledServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly ScheduledService _sut;

    public ScheduledServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        
        _sut = new ScheduledService(_dbContext, _httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCopyExercisesFromPlan_WhenPlanExists()
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

        var exercise = new PlanExercise
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Name = "Bench Press",
            RestSeconds = 120
        };

        exercise.Sets.Add(new PlanSet
        {
            Id = Guid.NewGuid(),
            PlanExerciseId = exercise.Id,
            SetNumber = 1,
            Reps = 8,
            Weight = 100
        });

        plan.Exercises.Add(exercise);

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.SaveChangesAsync();

        var dto = new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = new TimeOnly(14, 0),
            Status = "planned"
        };

        // Act
        var result = await _sut.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PlanId.Should().Be(plan.Id);
        result.Date.Should().Be(dto.Date);
        result.Exercises.Should().HaveCount(1);
        result.Exercises.First().Name.Should().Be("Bench Press");
        result.Exercises.First().Sets.Should().HaveCount(1);

        var scheduledInDb = await _dbContext.ScheduledWorkouts
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(s => s.Id == result.Id);
        scheduledInDb.Should().NotBeNull();
        scheduledInDb!.IsVisibleToFriends.Should().BeFalse(); // Default value
    }

    [Fact]
    public async Task CreateAsync_ShouldUseCustomExercises_WhenProvided()
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

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.SaveChangesAsync();

        var dto = new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = new TimeOnly(10, 0),
            Status = "planned",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto
                {
                    Name = "Custom Squat",
                    Rest = 180,
                    Sets = new List<SetDto>
                    {
                        new SetDto { Reps = 5, Weight = 140 }
                    }
                }
            }
        };

        // Act
        var result = await _sut.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Exercises.Should().HaveCount(1);
        result.Exercises.First().Name.Should().Be("Custom Squat");
        result.Exercises.First().Sets.First().Reps.Should().Be(5);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllScheduledWorkouts()
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

        var scheduled1 = new ScheduledWorkout
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

        var scheduled2 = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            UserId = userId,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = new TimeOnly(14, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Test Plan",
            Plan = plan
        };

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddRangeAsync(scheduled1, scheduled2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByDateAsync_ShouldFilterByDateCorrectly()
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

        var targetDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5));

        var scheduledOnTargetDate = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            UserId = userId,
            Date = targetDate,
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Test Plan",
            Plan = plan
        };

        var scheduledOtherDate = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            UserId = userId,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            Time = new TimeOnly(14, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Test Plan",
            Plan = plan
        };

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddRangeAsync(scheduledOnTargetDate, scheduledOtherDate);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetByDateAsync(targetDate, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Date.Should().Be(targetDate);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateScheduledWorkout_WhenUserIsOwner()
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

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();

        var updateDto = new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = new TimeOnly(14, 0),
            Status = "completed",
            Notes = "Updated notes"
        };

        // Act
        var result = await _sut.UpdateAsync(scheduled.Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Date.Should().Be(updateDto.Date);
        result.Time.Should().Be(updateDto.Time);
        result.Status.Should().Be("completed");
        result.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenUserIsNotOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var otherUserId = Guid.NewGuid();

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Test Plan",
            Type = "PPL",
            CreatedByUserId = otherUserId
        };

        var scheduled = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            UserId = otherUserId, // Different user
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Test Plan",
            Plan = plan
        };

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();

        var updateDto = new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = "planned"
        };

        // Act
        var result = await _sut.UpdateAsync(scheduled.Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteScheduledWorkout_WhenUserIsOwner()
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

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(scheduled.Id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var deletedScheduled = await _dbContext.ScheduledWorkouts.FindAsync(scheduled.Id);
        deletedScheduled.Should().BeNull();
    }

    [Fact]
    public async Task DuplicateAsync_ShouldCreateCopy_WhenScheduledExists()
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

        var original = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            UserId = userId,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Test Plan",
            Notes = "Original notes",
            IsVisibleToFriends = true,
            Plan = plan
        };

        var exercise = new ScheduledExercise
        {
            Id = Guid.NewGuid(),
            ScheduledWorkoutId = original.Id,
            Name = "Squat",
            RestSeconds = 180
        };

        exercise.Sets.Add(new ScheduledSet
        {
            Id = Guid.NewGuid(),
            ScheduledExerciseId = exercise.Id,
            SetNumber = 1,
            Reps = 5,
            Weight = 140
        });

        original.Exercises.Add(exercise);

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(original);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.DuplicateAsync(original.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(original.Id);
        result.PlanId.Should().Be(original.PlanId);
        result.Date.Should().Be(original.Date);
        result.Time.Should().Be(original.Time);
        result.Exercises.Should().HaveCount(1);
        result.Exercises.First().Name.Should().Be("Squat");
    }

    [Fact]
    public async Task CreateAsync_ShouldSetVisibleToFriends_WhenSpecified()
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

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.SaveChangesAsync();

        var dto = new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = new TimeOnly(14, 0),
            Status = "planned",
            VisibleToFriends = true
        };

        // Act
        var result = await _sut.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.VisibleToFriends.Should().BeTrue();

        var scheduledInDb = await _dbContext.ScheduledWorkouts.FindAsync(result.Id);
        scheduledInDb!.IsVisibleToFriends.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldChangeStatus_FromPlannedToCompleted()
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

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();

        var updateDto = new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = scheduled.Date,
            Time = scheduled.Time,
            Status = "completed"
        };

        // Act
        var result = await _sut.UpdateAsync(scheduled.Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("completed");

        var updatedInDb = await _dbContext.ScheduledWorkouts.FindAsync(scheduled.Id);
        updatedInDb!.Status.Should().Be(ScheduledStatus.Completed);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnScheduledWorkout_WhenExists()
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

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(scheduled.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(scheduled.Id);
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
