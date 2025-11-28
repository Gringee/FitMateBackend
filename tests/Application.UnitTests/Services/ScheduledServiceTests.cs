using Application.Abstractions;
using Application.DTOs;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Application.UnitTests.Services;

public class ScheduledServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly ScheduledService _sut;

    public ScheduledServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _currentUserMock = new Mock<ICurrentUserService>();
        
        _sut = new ScheduledService(_dbContext, _currentUserMock.Object);
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
    public async Task UpdateAsync_ShouldOnlyChangeDateAndTime_WhenNoOtherChanges()
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
            Plan = plan,
            Notes = "Original notes"
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

        var originalDate = scheduled.Date;
        var originalTime = scheduled.Time;
        var newDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var newTime = new TimeOnly(16, 30);

        var updateDto = new CreateScheduledDto
        {
            PlanId = plan.Id, // Same plan
            Date = newDate,
            Time = newTime,
            Status = "planned",
            Notes = "Original notes"
            // No Exercises provided - should keep existing
        };

        // Act
        var result = await _sut.UpdateAsync(scheduled.Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        
        // Date and Time changed
        result!.Date.Should().Be(newDate);
        result.Date.Should().NotBe(originalDate);
        result.Time.Should().Be(newTime);
        result.Time.Should().NotBe(originalTime);
        
        // Everything else unchanged
        result.PlanId.Should().Be(plan.Id);
        result.Status.Should().Be("planned");
        result.Notes.Should().Be("Original notes");
        
        // Exercises preserved
        result.Exercises.Should().HaveCount(1);
        result.Exercises.First().Name.Should().Be("Bench Press");
        result.Exercises.First().Rest.Should().Be(120);
        result.Exercises.First().Sets.Should().HaveCount(1);
        result.Exercises.First().Sets.First().Reps.Should().Be(8);
        result.Exercises.First().Sets.First().Weight.Should().Be(100);

        // Verify in DB
        var updatedInDb = await _dbContext.ScheduledWorkouts
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(s => s.Id == scheduled.Id);
        
        updatedInDb.Should().NotBeNull();
        updatedInDb!.Date.Should().Be(newDate);
        updatedInDb.Time.Should().Be(newTime);
        updatedInDb.Exercises.Should().HaveCount(1);
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


    [Fact]
    public async Task UpdateAsync_ShouldUpdateExercises_WhenExercisesProvided()
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
            Weight = 80
        });

        scheduled.Exercises.Add(exercise);

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();

        var updateDto = new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = scheduled.Date,
            Time = scheduled.Time,
            Status = "planned",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto
                {
                    Name = "Bench Press",
                    Rest = 120,
                    Sets = new List<SetDto>
                    {
                        new SetDto { Reps = 10, Weight = 100 } // Changed weight from 80 to 100
                    }
                }
            }
        };

        // Act
        var result = await _sut.UpdateAsync(scheduled.Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Exercises.Should().HaveCount(1);
        result.Exercises.First().Sets.Should().HaveCount(1);
        result.Exercises.First().Sets.First().Weight.Should().Be(100); // Updated weight
        result.Exercises.First().Sets.First().Reps.Should().Be(10); // Updated reps

        // Verify in DB
        var updatedInDb = await _dbContext.ScheduledWorkouts
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(s => s.Id == scheduled.Id);
        updatedInDb.Should().NotBeNull();
        updatedInDb!.Exercises.Should().HaveCount(1);
        updatedInDb.Exercises.First().Sets.First().Weight.Should().Be(100);
    }

    [Fact]
    public async Task UpdateAsync_ShouldCopyFromNewPlan_WhenPlanIdChanged()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var originalPlan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Original Plan",
            Type = "PPL",
            CreatedByUserId = userId
        };

        var newPlan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "New Plan",
            Type = "Upper/Lower",
            CreatedByUserId = userId
        };

        var newPlanExercise = new PlanExercise
        {
            Id = Guid.NewGuid(),
            PlanId = newPlan.Id,
            Name = "Squat",
            RestSeconds = 180
        };

        newPlanExercise.Sets.Add(new PlanSet
        {
            Id = Guid.NewGuid(),
            PlanExerciseId = newPlanExercise.Id,
            SetNumber = 1,
            Reps = 5,
            Weight = 140
        });

        newPlan.Exercises.Add(newPlanExercise);

        var scheduled = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            PlanId = originalPlan.Id,
            UserId = userId,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Original Plan",
            Plan = originalPlan
        };

        await _dbContext.Plans.AddRangeAsync(originalPlan, newPlan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();

        var updateDto = new CreateScheduledDto
        {
            PlanId = newPlan.Id, // Changed plan
            Date = scheduled.Date,
            Time = scheduled.Time,
            Status = "planned"
        };

        // Act
        var result = await _sut.UpdateAsync(scheduled.Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.PlanId.Should().Be(newPlan.Id);
        result.Exercises.Should().HaveCount(1);
        result.Exercises.First().Name.Should().Be("Squat"); // From new plan
        result.Exercises.First().Sets.First().Weight.Should().Be(140);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUseCustomExercises_WhenPlanChangedButExercisesProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var originalPlan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Original Plan",
            Type = "PPL",
            CreatedByUserId = userId
        };

        var newPlan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "New Plan",
            Type = "Upper/Lower",
            CreatedByUserId = userId
        };

        var scheduled = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            PlanId = originalPlan.Id,
            UserId = userId,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Original Plan",
            Plan = originalPlan
        };

        await _dbContext.Plans.AddRangeAsync(originalPlan, newPlan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();

        var updateDto = new CreateScheduledDto
        {
            PlanId = newPlan.Id, // Changed plan
            Date = scheduled.Date,
            Time = scheduled.Time,
            Status = "planned",
            Exercises = new List<ExerciseDto> // But also provide custom exercises
            {
                new ExerciseDto
                {
                    Name = "Custom Deadlift",
                    Rest = 240,
                    Sets = new List<SetDto>
                    {
                        new SetDto { Reps = 3, Weight = 200 }
                    }
                }
            }
        };

        // Act
        var result = await _sut.UpdateAsync(scheduled.Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.PlanId.Should().Be(newPlan.Id);
        result.Exercises.Should().HaveCount(1);
        result.Exercises.First().Name.Should().Be("Custom Deadlift"); // Custom, not from new plan
        result.Exercises.First().Sets.First().Weight.Should().Be(200);
    }

    [Fact]
    public async Task UpdateAsync_ShouldKeepExistingExercises_WhenNoExercisesProvidedAndPlanUnchanged()
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
            Weight = 80
        });

        scheduled.Exercises.Add(exercise);

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);
        await _dbContext.SaveChangesAsync();

        var updateDto = new CreateScheduledDto
        {
            PlanId = plan.Id, // Same plan
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), // Only change date
            Time = new TimeOnly(14, 0), // And time
            Status = "planned"
            // No Exercises provided
        };

        // Act
        var result = await _sut.UpdateAsync(scheduled.Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Date.Should().Be(updateDto.Date);
        result.Time.Should().Be(updateDto.Time);
        result.Exercises.Should().HaveCount(1); // Original exercises preserved
        result.Exercises.First().Name.Should().Be("Bench Press");
        result.Exercises.First().Sets.First().Weight.Should().Be(80); // Original weight
    }

    [Fact]
    public async Task UpdateAsync_ShouldHandleMultipleExercises_WithMultipleSets()
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
            Status = "planned",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto
                {
                    Name = "Squat",
                    Rest = 180,
                    Sets = new List<SetDto>
                    {
                        new SetDto { Reps = 5, Weight = 100 },
                        new SetDto { Reps = 5, Weight = 110 },
                        new SetDto { Reps = 5, Weight = 120 }
                    }
                },
                new ExerciseDto
                {
                    Name = "Bench Press",
                    Rest = 120,
                    Sets = new List<SetDto>
                    {
                        new SetDto { Reps = 8, Weight = 80 },
                        new SetDto { Reps = 8, Weight = 85 }
                    }
                }
            }
        };

        // Act
        var result = await _sut.UpdateAsync(scheduled.Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Exercises.Should().HaveCount(2);
        
        var squat = result.Exercises.First(e => e.Name == "Squat");
        squat.Sets.Should().HaveCount(3);
        squat.Sets.ElementAt(0).Weight.Should().Be(100);
        squat.Sets.ElementAt(1).Weight.Should().Be(110);
        squat.Sets.ElementAt(2).Weight.Should().Be(120);

        var bench = result.Exercises.First(e => e.Name == "Bench Press");
        bench.Sets.Should().HaveCount(2);

        // Verify in DB - no orphaned exercises/sets
        var allExercises = await _dbContext.ScheduledExercises.ToListAsync();
        var exercisesForThisScheduled = allExercises.Where(e => e.ScheduledWorkoutId == scheduled.Id).ToList();
        exercisesForThisScheduled.Should().HaveCount(2); // Only the new ones
    }

    // Helper method
    private void SetupAuthenticatedUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }
}
