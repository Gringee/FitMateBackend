using Application.Abstractions;
using Application.DTOs;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Infrastructure.Persistence;
using Moq;
using System.Linq.Expressions;

namespace Application.UnitTests.Services;

public class PlanServiceTests
{
    private readonly AppDbContext _dbContext; // Use real DbContext with InMemory provider
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly PlanService _sut;

    public PlanServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        
        // PlanService expects IApplicationDbContext, which AppDbContext implements
        _sut = new PlanService(_dbContext, _httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreatePlan_WhenValidRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())
        }));
        
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var dto = new CreatePlanDto 
        { 
            PlanName = "Test Plan", 
            Type = "Split", 
            Notes = "Notes", 
            Exercises = new List<ExerciseDto>() 
        };

        // Act
        var result = await _sut.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PlanName.Should().Be(dto.PlanName);
        result.Type.Should().Be(dto.Type);

        var planInDb = await _dbContext.Plans.FirstOrDefaultAsync(p => p.Id == result.Id);
        planInDb.Should().NotBeNull();
        planInDb!.CreatedByUserId.Should().Be(userId);
        planInDb.PlanName.Should().Be(dto.PlanName);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUsersPlans_WhenIncludeSharedIsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var userPlan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "My Plan",
            Type = "PPL",
            CreatedByUserId = userId
        };

        var otherUserPlan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Other Plan",
            Type = "Bro Split",
            CreatedByUserId = Guid.NewGuid()
        };

        await _dbContext.Plans.AddRangeAsync(userPlan, otherUserPlan);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync(includeShared: false, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().PlanName.Should().Be("My Plan");
    }

    [Fact]
    public async Task GetAllAsync_ShouldIncludeSharedPlans_WhenIncludeSharedIsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var sharedByUser = Guid.NewGuid();

        var ownPlan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Own Plan",
            Type = "PPL",
            CreatedByUserId = userId
        };

        var sharedPlan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Shared Plan",
            Type = "Upper/Lower",
            CreatedByUserId = sharedByUser
        };

        var sharedPlanEntry = new SharedPlan
        {
            Id = Guid.NewGuid(),
            PlanId = sharedPlan.Id,
            SharedByUserId = sharedByUser,
            SharedWithUserId = userId,
            Status = Domain.Enums.RequestStatus.Accepted,
            SharedAtUtc = DateTime.UtcNow,
            Plan = sharedPlan
        };

        await _dbContext.Plans.AddRangeAsync(ownPlan, sharedPlan);
        await _dbContext.SharedPlans.AddAsync(sharedPlanEntry);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync(includeShared: true, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.PlanName == "Own Plan");
        result.Should().Contain(p => p.PlanName == "Shared Plan");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPlan_WhenUserIsOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "My Detailed Plan",
            Type = "Full Body",
            CreatedByUserId = userId
        };

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(plan.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.PlanName.Should().Be("My Detailed Plan");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserIsNotOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var otherUserPlan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Someone Else's Plan",
            Type = "Push/Pull",
            CreatedByUserId = Guid.NewGuid()
        };

        await _dbContext.Plans.AddAsync(otherUserPlan);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(otherUserPlan.Id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeletePlan_WhenUserIsOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Plan To Delete",
            Type = "PPL",
            CreatedByUserId = userId
        };

        await _dbContext.Plans.AddAsync(plan);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(plan.Id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var deletedPlan = await _dbContext.Plans.FindAsync(plan.Id);
        deletedPlan.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenUserIsNotOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var otherUserPlan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Someone Else's Plan",
            Type = "Bro Split",
            CreatedByUserId = Guid.NewGuid()
        };

        await _dbContext.Plans.AddAsync(otherUserPlan);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(otherUserPlan.Id, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        var plan = await _dbContext.Plans.FindAsync(otherUserPlan.Id);
        plan.Should().NotBeNull("plan should not be deleted");
    }

    [Fact]
    public async Task DuplicateAsync_ShouldCreateCopy_WhenPlanExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var originalPlan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = "Original Plan",
            Type = "PPL",
            Notes = "Original Notes",
            CreatedByUserId = userId
        };

        var exercise = new PlanExercise
        {
            Id = Guid.NewGuid(),
            PlanId = originalPlan.Id,
            Name = "Bench Press",
            RestSeconds = 90
        };

        exercise.Sets.Add(new PlanSet
        {
            Id = Guid.NewGuid(),
            PlanExerciseId = exercise.Id,
            SetNumber = 1,
            Reps = 8,
            Weight = 100
        });

        originalPlan.Exercises.Add(exercise);

        await _dbContext.Plans.AddAsync(originalPlan);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.DuplicateAsync(originalPlan.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PlanName.Should().Be("Original Plan (Copy)");
        result.Id.Should().NotBe(originalPlan.Id);
        result.Exercises.Should().HaveCount(1);

        var copiedPlanInDb = await _dbContext.Plans
            .Include(p => p.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(p => p.Id == result.Id);
        copiedPlanInDb.Should().NotBeNull();
        copiedPlanInDb!.Exercises.Should().HaveCount(1);
        copiedPlanInDb.Exercises.First().Sets.Should().HaveCount(1);
    }

    // Helper method to setup authenticated user
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

