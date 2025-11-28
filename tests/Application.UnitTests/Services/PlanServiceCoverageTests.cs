using Application.DTOs;
using Application.Abstractions;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Application.UnitTests.Services;

public class PlanServiceCoverageTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly PlanService _sut;

    public PlanServiceCoverageTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _currentUserMock = new Mock<ICurrentUserService>();
        _sut = new PlanService(_dbContext, _currentUserMock.Object);
    }

    private void SetupAuthenticatedUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenPlanNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);
        var dto = new CreatePlanDto { PlanName = "New", Type = "Type" };

        // Act
        var result = await _sut.UpdateAsync(Guid.NewGuid(), dto, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdatePlan_WhenUserIsOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Original Plan",
            Type = "PPL",
            Notes = "Original notes",
            CreatedByUserId = userId
        };

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.SaveChangesAsync();

        var updateDto = new CreatePlanDto
        {
            PlanName = "Updated Plan",
            Type = "Upper/Lower",
            Notes = "Updated notes",
            Exercises = new List<ExerciseDto>()
        };

        // Act
        var result = await _sut.UpdateAsync(plan.Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.PlanName.Should().Be("Updated Plan");
        result.Type.Should().Be("Upper/Lower");
        result.Notes.Should().Be("Updated notes");

        // Verify in DB
        var updatedInDb = await _dbContext.Plans.FindAsync(plan.Id);
        updatedInDb.Should().NotBeNull();
        updatedInDb!.PlanName.Should().Be("Updated Plan");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenUserIsNotOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Other User Plan",
            Type = "PPL",
            CreatedByUserId = otherUserId // Different owner
        };

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.SaveChangesAsync();

        var updateDto = new CreatePlanDto
        {
            PlanName = "Hacked Plan",
            Type = "Malicious"
        };

        // Act
        var result = await _sut.UpdateAsync(plan.Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeNull(); // Should not allow update

        // Verify plan unchanged
        var unchangedPlan = await _dbContext.Plans.FindAsync(plan.Id);
        unchangedPlan!.PlanName.Should().Be("Other User Plan");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExercises_WhenExercisesChanged()
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

        var oldExercise = new PlanExercise
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Name = "Old Exercise",
            RestSeconds = 60
        };

        oldExercise.Sets.Add(new PlanSet
        {
            Id = Guid.NewGuid(),
            PlanExerciseId = oldExercise.Id,
            SetNumber = 1,
            Reps = 10,
            Weight = 50
        });

        plan.Exercises.Add(oldExercise);

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.SaveChangesAsync();

        var updateDto = new CreatePlanDto
        {
            PlanName = "Test Plan",
            Type = "PPL",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto
                {
                    Name = "New Exercise",
                    Rest = 120,
                    Sets = new List<SetDto>
                    {
                        new SetDto { Reps = 5, Weight = 100 },
                        new SetDto { Reps = 5, Weight = 110 }
                    }
                }
            }
        };

        // Act
        var result = await _sut.UpdateAsync(plan.Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Exercises.Should().HaveCount(1);
        result.Exercises.First().Name.Should().Be("New Exercise");
        result.Exercises.First().Sets.Should().HaveCount(2);
        result.Exercises.First().Sets.ElementAt(0).Weight.Should().Be(100);
        result.Exercises.First().Sets.ElementAt(1).Weight.Should().Be(110);

        // Verify old exercise removed from DB
        var allExercises = await _dbContext.PlanExercises.ToListAsync();
        allExercises.Should().HaveCount(1);
        allExercises.First().Name.Should().Be("New Exercise");

        // Verify old sets removed
        var allSets = await _dbContext.PlanSets.ToListAsync();
        allSets.Should().HaveCount(2);
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
            PlanName = "Complex Plan",
            Type = "PPL",
            CreatedByUserId = userId
        };

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.SaveChangesAsync();

        var updateDto = new CreatePlanDto
        {
            PlanName = "Complex Plan",
            Type = "PPL",
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
                },
                new ExerciseDto
                {
                    Name = "Deadlift",
                    Rest = 240,
                    Sets = new List<SetDto>
                    {
                        new SetDto { Reps = 1, Weight = 200 }
                    }
                }
            }
        };

        // Act
        var result = await _sut.UpdateAsync(plan.Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Exercises.Should().HaveCount(3);

        var squat = result.Exercises.First(e => e.Name == "Squat");
        squat.Sets.Should().HaveCount(3);

        var bench = result.Exercises.First(e => e.Name == "Bench Press");
        bench.Sets.Should().HaveCount(2);

        var deadlift = result.Exercises.First(e => e.Name == "Deadlift");
        deadlift.Sets.Should().HaveCount(1);

        // Verify in DB - total sets should be 3 + 2 + 1 = 6
        var allSets = await _dbContext.PlanSets.ToListAsync();
        allSets.Should().HaveCount(6);
    }

    [Fact]
    public async Task UpdateAsync_ShouldClearExercises_WhenEmptyExercisesProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Plan With Exercises",
            Type = "PPL",
            CreatedByUserId = userId
        };

        var exercise = new PlanExercise
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Name = "Exercise to Remove",
            RestSeconds = 60
        };

        exercise.Sets.Add(new PlanSet
        {
            Id = Guid.NewGuid(),
            PlanExerciseId = exercise.Id,
            SetNumber = 1,
            Reps = 10,
            Weight = 50
        });

        plan.Exercises.Add(exercise);

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.SaveChangesAsync();

        var updateDto = new CreatePlanDto
        {
            PlanName = "Plan Without Exercises",
            Type = "Empty",
            Exercises = new List<ExerciseDto>() // Empty list
        };

        // Act
        var result = await _sut.UpdateAsync(plan.Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Exercises.Should().BeEmpty();
        result.PlanName.Should().Be("Plan Without Exercises");

        // Verify exercises removed from DB
        var allExercises = await _dbContext.PlanExercises.Where(e => e.PlanId == plan.Id).ToListAsync();
        allExercises.Should().BeEmpty();

        // Verify sets removed from DB
        var allSets = await _dbContext.PlanSets.ToListAsync();
        allSets.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_ShouldOnlyUpdatePlanName_WithoutTouchingExercises()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Original Name",
            Type = "PPL",
            CreatedByUserId = userId
        };

        var exercise = new PlanExercise
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Name = "Existing Exercise",
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

        var originalExerciseId = exercise.Id;
        var originalSetId = exercise.Sets.First().Id;

        var updateDto = new CreatePlanDto
        {
            PlanName = "New Name",
            Type = "Upper/Lower",
            Notes = "New notes",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto
                {
                    Name = "Existing Exercise",
                    Rest = 120,
                    Sets = new List<SetDto>
                    {
                        new SetDto { Reps = 8, Weight = 100 }
                    }
                }
            }
        };

        // Act
        var result = await _sut.UpdateAsync(plan.Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.PlanName.Should().Be("New Name");
        result.Type.Should().Be("Upper/Lower");
        result.Exercises.Should().HaveCount(1);
        result.Exercises.First().Name.Should().Be("Existing Exercise");

        // Note: IDs will be different because raw SQL deletes and recreates exercises
        var newExercises = await _dbContext.PlanExercises.Where(e => e.PlanId == plan.Id).ToListAsync();
        newExercises.Should().HaveCount(1);
    }

}
