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

public class FriendshipServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly FriendshipService _sut;

    public FriendshipServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        
        _sut = new FriendshipService(_dbContext, _httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task SendRequestAsync_ShouldCreatePendingRequest_WhenNoRelationshipExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        // Act
        await _sut.SendRequestAsync(friendId, CancellationToken.None);

        // Assert
        var friendship = await _dbContext.Friendships.FirstOrDefaultAsync();
        friendship.Should().NotBeNull();
        friendship!.Status.Should().Be(RequestStatus.Pending);
        friendship.RequestedByUserId.Should().Be(userId);
        
        // Canonical pair check
        var (a, b) = userId.CompareTo(friendId) < 0 ? (userId, friendId) : (friendId, userId);
        friendship.UserAId.Should().Be(a);
        friendship.UserBId.Should().Be(b);
    }

    [Fact]
    public async Task SendRequestAsync_ShouldThrow_WhenSendingToSelf()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        // Act
        Func<Task> act = async () => await _sut.SendRequestAsync(userId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("You cannot add yourself.");
    }

    [Fact]
    public async Task SendRequestAsync_ShouldThrow_WhenAlreadyFriends()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var (a, b) = userId.CompareTo(friendId) < 0 ? (userId, friendId) : (friendId, userId);
        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            UserAId = a,
            UserBId = b,
            RequestedByUserId = userId,
            Status = RequestStatus.Accepted
        };
        await _dbContext.Friendships.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _sut.SendRequestAsync(friendId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("You are already friends.");
    }

    [Fact]
    public async Task SendRequestAsync_ShouldAutoAccept_WhenRequestPendingFromOtherUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var (a, b) = userId.CompareTo(friendId) < 0 ? (userId, friendId) : (friendId, userId);
        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            UserAId = a,
            UserBId = b,
            RequestedByUserId = friendId, // Requested by OTHER user
            Status = RequestStatus.Pending
        };
        await _dbContext.Friendships.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        await _sut.SendRequestAsync(friendId, CancellationToken.None);

        // Assert
        var updated = await _dbContext.Friendships.FindAsync(friendship.Id);
        updated!.Status.Should().Be(RequestStatus.Accepted);
        updated.RespondedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task RespondAsync_ShouldAcceptRequest_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var (a, b) = userId.CompareTo(friendId) < 0 ? (userId, friendId) : (friendId, userId);
        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            UserAId = a,
            UserBId = b,
            RequestedByUserId = friendId, // Requested by friend
            Status = RequestStatus.Pending
        };
        await _dbContext.Friendships.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        await _sut.RespondAsync(friendship.Id, true, CancellationToken.None);

        // Assert
        var updated = await _dbContext.Friendships.FindAsync(friendship.Id);
        updated!.Status.Should().Be(RequestStatus.Accepted);
        updated.RespondedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task RespondAsync_ShouldRejectRequest_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var (a, b) = userId.CompareTo(friendId) < 0 ? (userId, friendId) : (friendId, userId);
        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            UserAId = a,
            UserBId = b,
            RequestedByUserId = friendId,
            Status = RequestStatus.Pending
        };
        await _dbContext.Friendships.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        await _sut.RespondAsync(friendship.Id, false, CancellationToken.None);

        // Assert
        var updated = await _dbContext.Friendships.FindAsync(friendship.Id);
        updated!.Status.Should().Be(RequestStatus.Rejected);
    }

    [Fact]
    public async Task RespondAsync_ShouldThrow_WhenRespondingToOwnRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var (a, b) = userId.CompareTo(friendId) < 0 ? (userId, friendId) : (friendId, userId);
        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            UserAId = a,
            UserBId = b,
            RequestedByUserId = userId, // Requested by ME
            Status = RequestStatus.Pending
        };
        await _dbContext.Friendships.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _sut.RespondAsync(friendship.Id, true, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("You cannot respond to your own friend request.");
    }

    [Fact]
    public async Task GetFriendsAsync_ShouldReturnFriendList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId1 = Guid.NewGuid();
        var friendId2 = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var friend1 = new User { Id = friendId1, UserName = "friend1", FullName = "Friend One", Email = "f1@e.com", PasswordHash = "h" };
        var friend2 = new User { Id = friendId2, UserName = "friend2", FullName = "Friend Two", Email = "f2@e.com", PasswordHash = "h" };
        await _dbContext.Users.AddRangeAsync(friend1, friend2);

        // Friendship 1 (Accepted)
        var (a1, b1) = userId.CompareTo(friendId1) < 0 ? (userId, friendId1) : (friendId1, userId);
        await _dbContext.Friendships.AddAsync(new Friendship { Id = Guid.NewGuid(), UserAId = a1, UserBId = b1, Status = RequestStatus.Accepted });

        // Friendship 2 (Pending - not a friend yet)
        var (a2, b2) = userId.CompareTo(friendId2) < 0 ? (userId, friendId2) : (friendId2, userId);
        await _dbContext.Friendships.AddAsync(new Friendship { Id = Guid.NewGuid(), UserAId = a2, UserBId = b2, Status = RequestStatus.Pending });

        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetFriendsAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().UserName.Should().Be("friend1");
    }

    [Fact]
    public async Task RemoveFriendAsync_ShouldRemoveFriendship()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var (a, b) = userId.CompareTo(friendId) < 0 ? (userId, friendId) : (friendId, userId);
        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            UserAId = a,
            UserBId = b,
            Status = RequestStatus.Accepted
        };
        await _dbContext.Friendships.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.RemoveFriendAsync(friendId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var deleted = await _dbContext.Friendships.FindAsync(friendship.Id);
        deleted.Should().BeNull();
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
    [Fact]
    public async Task GetIncomingRequestsAsync_ShouldReturnPendingRequests()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var friend = new User { Id = friendId, UserName = "friend", FullName = "Friend", Email = "f@e.com", PasswordHash = "h" };
        await _dbContext.Users.AddAsync(friend);

        var (a, b) = userId.CompareTo(friendId) < 0 ? (userId, friendId) : (friendId, userId);
        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            UserAId = a,
            UserBId = b,
            RequestedByUserId = friendId, // Requested by friend, so it's incoming for user
            Status = RequestStatus.Pending
        };
        await _dbContext.Friendships.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetIncomingAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(friendship.Id);
        result.First().FromName.Should().Be("Friend");
    }

    [Fact]
    public async Task GetOutgoingRequestsAsync_ShouldReturnPendingRequests()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var friend = new User { Id = friendId, UserName = "friend", FullName = "Friend", Email = "f@e.com", PasswordHash = "h" };
        await _dbContext.Users.AddAsync(friend);

        var (a, b) = userId.CompareTo(friendId) < 0 ? (userId, friendId) : (friendId, userId);
        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            UserAId = a,
            UserBId = b,
            RequestedByUserId = userId, // Requested by user, so it's outgoing
            Status = RequestStatus.Pending
        };
        await _dbContext.Friendships.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetOutgoingAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(friendship.Id);
        result.First().ToName.Should().Be("Friend");
    }

    [Fact]
    public async Task AreFriendsAsync_ShouldReturnTrue_WhenFriends()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var (a, b) = userId.CompareTo(friendId) < 0 ? (userId, friendId) : (friendId, userId);
        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            UserAId = a,
            UserBId = b,
            Status = RequestStatus.Accepted
        };
        await _dbContext.Friendships.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.AreFriendsAsync(userId, friendId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetFriendshipIdAsync_ShouldReturnId_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        SetupAuthenticatedUser(userId);

        var (a, b) = userId.CompareTo(friendId) < 0 ? (userId, friendId) : (friendId, userId);
        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            UserAId = a,
            UserBId = b,
            Status = RequestStatus.Accepted
        };
        await _dbContext.Friendships.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetFriendshipIdAsync(userId, friendId, CancellationToken.None);

        // Assert
        result.Should().Be(friendship.Id);
    }
}
