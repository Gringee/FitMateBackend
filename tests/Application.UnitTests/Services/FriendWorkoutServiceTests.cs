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

public class FriendWorkoutServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IFriendshipService> _friendshipServiceMock;
    private readonly FriendWorkoutService _sut;

    public FriendWorkoutServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _currentUserMock = new Mock<ICurrentUserService>();
        _friendshipServiceMock = new Mock<IFriendshipService>();
        
        _sut = new FriendWorkoutService(_dbContext, _currentUserMock.Object, _friendshipServiceMock.Object);
    }

    [Fact]
    public async Task GetFriendsScheduledAsync_ShouldReturnWorkouts_WhenVisibleAndWithinDateRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        // Setup friendship mock
        _friendshipServiceMock.Setup(x => x.GetFriendIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { friendId });

        // Create friend user
        var friend = new User { Id = friendId, UserName = "friend", FullName = "Friend User", Email = "f@e.com", PasswordHash = "h" };
        await _dbContext.Users.AddAsync(friend);

        // Create scheduled workouts
        var plan = new Plan { Id = Guid.NewGuid(), PlanName = "Plan", Type = "PPL", CreatedByUserId = friendId };
        await _dbContext.Plans.AddAsync(plan);

        var workout1 = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            UserId = friendId,
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Plan",
            IsVisibleToFriends = true,
            Plan = plan,
            User = friend
        };

        var workout2 = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            UserId = friendId,
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Plan",
            IsVisibleToFriends = false, // Not visible
            Plan = plan,
            User = friend
        };

        var workout3 = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            UserId = friendId,
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(10)), // Outside range
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Plan",
            IsVisibleToFriends = true,
            Plan = plan,
            User = friend
        };

        await _dbContext.ScheduledWorkouts.AddRangeAsync(workout1, workout2, workout3);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetFriendsScheduledAsync(
            DateOnly.FromDateTime(DateTime.Today), 
            DateOnly.FromDateTime(DateTime.Today.AddDays(5)), 
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().ScheduledId.Should().Be(workout1.Id);
        result.First().UserName.Should().Be("friend");
    }

    [Fact]
    public async Task GetFriendsScheduledAsync_ShouldReturnEmpty_WhenNoFriends()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        _friendshipServiceMock.Setup(x => x.GetFriendIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        // Act
        var result = await _sut.GetFriendsScheduledAsync(
            DateOnly.FromDateTime(DateTime.Today), 
            DateOnly.FromDateTime(DateTime.Today.AddDays(5)), 
            CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFriendsSessionsAsync_ShouldReturnSessions_WhenVisibleAndWithinDateRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        _friendshipServiceMock.Setup(x => x.GetFriendIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { friendId });

        var friend = new User { Id = friendId, UserName = "friend", FullName = "Friend User", Email = "f@e.com", PasswordHash = "h" };
        await _dbContext.Users.AddAsync(friend);

        var plan = new Plan { Id = Guid.NewGuid(), PlanName = "Plan", Type = "PPL", CreatedByUserId = friendId };
        await _dbContext.Plans.AddAsync(plan);

        var scheduled = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            UserId = friendId,
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Completed,
            PlanName = "Plan",
            IsVisibleToFriends = true,
            Plan = plan,
            User = friend
        };
        await _dbContext.ScheduledWorkouts.AddAsync(scheduled);

        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            ScheduledId = scheduled.Id,
            UserId = friendId,
            StartedAtUtc = DateTime.UtcNow,
            Status = WorkoutSessionStatus.Completed,
            Scheduled = scheduled
        };
        await _dbContext.WorkoutSessions.AddAsync(session);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetFriendsSessionsAsync(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1),
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().SessionId.Should().Be(session.Id);
        result.First().UserName.Should().Be("friend");
    }

    [Fact]
    public async Task GetFriendsScheduledAsync_ShouldExcludeNonFriends()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        var nonFriendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        // Setup friendship mock - only friendId
        _friendshipServiceMock.Setup(x => x.GetFriendIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { friendId });

        var friend = new User { Id = friendId, UserName = "friend", FullName = "Friend", Email = "f@e.com", PasswordHash = "h" };
        var nonFriend = new User { Id = nonFriendId, UserName = "nonfriend", FullName = "NonFriend", Email = "nf@e.com", PasswordHash = "h" };
        await _dbContext.Users.AddRangeAsync(friend, nonFriend);

        var plan1 = new Plan { Id = Guid.NewGuid(), PlanName = "Plan1", Type = "PPL", CreatedByUserId = friendId };
        var plan2 = new Plan { Id = Guid.NewGuid(), PlanName = "Plan2", Type = "PPL", CreatedByUserId = nonFriendId };
        await _dbContext.Plans.AddRangeAsync(plan1, plan2);

        var friendWorkout = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            UserId = friendId,
            PlanId = plan1.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Plan1",
            IsVisibleToFriends = true,
            Plan = plan1,
            User = friend
        };

        var nonFriendWorkout = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            UserId = nonFriendId,
            PlanId = plan2.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Plan2",
            IsVisibleToFriends = true,
            Plan = plan2,
            User = nonFriend
        };

        await _dbContext.ScheduledWorkouts.AddRangeAsync(friendWorkout, nonFriendWorkout);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetFriendsScheduledAsync(
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().UserName.Should().Be("friend");
    }

    [Fact]
    public async Task GetFriendsScheduledAsync_ShouldRespectVisibleToFriendsFlag()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        _friendshipServiceMock.Setup(x => x.GetFriendIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { friendId });

        var friend = new User { Id = friendId, UserName = "friend", FullName = "Friend", Email = "f@e.com", PasswordHash = "h" };
        await _dbContext.Users.AddAsync(friend);

        var plan = new Plan { Id = Guid.NewGuid(), PlanName = "Plan", Type = "PPL", CreatedByUserId = friendId };
        await _dbContext.Plans.AddAsync(plan);

        var visibleWorkout = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            UserId = friendId,
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Plan",
            IsVisibleToFriends = true,
            Plan = plan,
            User = friend
        };

        var hiddenWorkout = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            UserId = friendId,
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = new TimeOnly(11, 0),
            Status = ScheduledStatus.Planned,
            PlanName = "Plan",
            IsVisibleToFriends = false,
            Plan = plan,
            User = friend
        };

        await _dbContext.ScheduledWorkouts.AddRangeAsync(visibleWorkout, hiddenWorkout);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetFriendsScheduledAsync(
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().ScheduledId.Should().Be(visibleWorkout.Id);
    }

    [Fact]
    public async Task GetFriendsSessionsAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        _friendshipServiceMock.Setup(x => x.GetFriendIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { friendId });

        var friend = new User { Id = friendId, UserName = "friend", FullName = "Friend", Email = "f@e.com", PasswordHash = "h" };
        await _dbContext.Users.AddAsync(friend);

        var plan = new Plan { Id = Guid.NewGuid(), PlanName = "Plan", Type = "PPL", CreatedByUserId = friendId };
        await _dbContext.Plans.AddAsync(plan);

        var scheduled1 = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            UserId = friendId,
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Completed,
            PlanName = "Plan",
            IsVisibleToFriends = true,
            Plan = plan,
            User = friend
        };

        var scheduled2 = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            UserId = friendId,
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            Time = new TimeOnly(10, 0),
            Status = ScheduledStatus.Completed,
            PlanName = "Plan",
            IsVisibleToFriends = true,
            Plan = plan,
            User = friend
        };

        await _dbContext.ScheduledWorkouts.AddRangeAsync(scheduled1, scheduled2);

        var sessionInRange = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            ScheduledId = scheduled1.Id,
            UserId = friendId,
            StartedAtUtc = DateTime.UtcNow,
            Status = WorkoutSessionStatus.Completed,
            Scheduled = scheduled1
        };

        var sessionOutOfRange = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            ScheduledId = scheduled2.Id,
            UserId = friendId,
            StartedAtUtc = DateTime.UtcNow.AddDays(10),
            Status = WorkoutSessionStatus.Completed,
            Scheduled = scheduled2
        };

        await _dbContext.WorkoutSessions.AddRangeAsync(sessionInRange, sessionOutOfRange);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetFriendsSessionsAsync(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(5),
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().SessionId.Should().Be(sessionInRange.Id);
    }

    // Helper method
    private void SetupAuthenticatedUser(Guid userId)
    {
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
    }
}
