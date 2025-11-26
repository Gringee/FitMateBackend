using Application.Abstractions;
using Application.DTOs;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Application.UnitTests.Services;

public class UserServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IUserValidationHelpers> _validationHelpersMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _validationHelpersMock = new Mock<IUserValidationHelpers>();
        
        _sut = new UserService(_dbContext, _validationHelpersMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsers_WhenNoSearchProvided()
    {
        // Arrange
        var user1 = new User { Id = Guid.NewGuid(), UserName = "user1", Email = "user1@test.com", FullName = "User One", PasswordHash = "hash" };
        var user2 = new User { Id = Guid.NewGuid(), UserName = "user2", Email = "user2@test.com", FullName = "User Two", PasswordHash = "hash" };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync(null, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.UserName == "user1");
        result.Should().Contain(u => u.UserName == "user2");
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterUsers_WhenSearchProvided()
    {
        // Arrange
        var user1 = new User { Id = Guid.NewGuid(), UserName = "john", Email = "john@test.com", FullName = "John Doe", PasswordHash = "hash" };
        var user2 = new User { Id = Guid.NewGuid(), UserName = "jane", Email = "jane@test.com", FullName = "Jane Smith", PasswordHash = "hash" };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync("john", CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        result.First().UserName.Should().Be("john");
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateUser_WhenValid()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            UserName = "newuser",
            Email = "newuser@test.com",
            FullName = "New User"
        };

        _validationHelpersMock.Setup(x => x.EnsureUserNameIsUniqueAsync("newuser", It.IsAny<CancellationToken>(), null))
            .Returns(Task.CompletedTask);
        _validationHelpersMock.Setup(x => x.EnsureEmailIsUniqueAsync("newuser@test.com", It.IsAny<CancellationToken>(), null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserName.Should().Be("newuser");
        result.Email.Should().Be("newuser@test.com");
        
        var userInDb = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == "newuser");
        userInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenUsernameTaken()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            UserName = "existinguser",
            Email = "new@test.com",
            FullName = "New User"
        };

        _validationHelpersMock.Setup(x => x.EnsureUserNameIsUniqueAsync("existinguser", It.IsAny<CancellationToken>(), null))
            .ThrowsAsync(new InvalidOperationException("Username already exists"));

        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Username already exists");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenEmailTaken()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            UserName = "newuser",
            Email = "existing@test.com",
            FullName = "New User"
        };

        _validationHelpersMock.Setup(x => x.EnsureUserNameIsUniqueAsync("newuser", It.IsAny<CancellationToken>(), null))
            .Returns(Task.CompletedTask);
        _validationHelpersMock.Setup(x => x.EnsureEmailIsUniqueAsync("existing@test.com", It.IsAny<CancellationToken>(), null))
            .ThrowsAsync(new InvalidOperationException("Email already exists"));

        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already exists");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUser_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "olduser", Email = "old@test.com", FullName = "Old Name", PasswordHash = "hash" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var dto = new UpdateUserDto
        {
            UserName = "newusername",
            Email = "new@test.com",
            FullName = "New Name"
        };

        _validationHelpersMock.Setup(x => x.EnsureUserNameIsUniqueAsync("newusername", It.IsAny<CancellationToken>(), userId))
            .Returns(Task.CompletedTask);
        _validationHelpersMock.Setup(x => x.EnsureEmailIsUniqueAsync("new@test.com", It.IsAny<CancellationToken>(), userId))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateAsync(userId, dto, CancellationToken.None);

        // Assert
        var updatedUser = await _dbContext.Users.FindAsync(userId);
        updatedUser!.UserName.Should().Be("newusername");
        updatedUser.Email.Should().Be("new@test.com");
        updatedUser.FullName.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new UpdateUserDto { UserName = "newname" };

        // Act
        Func<Task> act = async () => await _sut.UpdateAsync(userId, dto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenUsernameTaken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "original", Email = "original@test.com", FullName = "Original", PasswordHash = "hash" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var dto = new UpdateUserDto { UserName = "taken" };

        _validationHelpersMock.Setup(x => x.EnsureUserNameIsUniqueAsync("taken", It.IsAny<CancellationToken>(), userId))
            .ThrowsAsync(new InvalidOperationException("Username taken"));

        // Act
        Func<Task> act = async () => await _sut.UpdateAsync(userId, dto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Username taken");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenEmailTaken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "original", Email = "original@test.com", FullName = "Original", PasswordHash = "hash" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var dto = new UpdateUserDto { Email = "taken@test.com" };

        _validationHelpersMock.Setup(x => x.EnsureEmailIsUniqueAsync("taken@test.com", It.IsAny<CancellationToken>(), userId))
            .ThrowsAsync(new InvalidOperationException("Email taken"));

        // Act
        Func<Task> act = async () => await _sut.UpdateAsync(userId, dto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email taken");
    }

    [Fact]
    public async Task UpdateAsync_ShouldOnlyUpdateProvidedFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User 
        { 
            Id = userId, 
            UserName = "original", 
            Email = "original@test.com", 
            FullName = "Original Name",
            PasswordHash = "hash"
        };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Only update FullName, leave others null/empty
        var dto = new UpdateUserDto 
        { 
            FullName = "Updated Name" 
            // UserName and Email are null
        };

        // Act
        await _sut.UpdateAsync(userId, dto, CancellationToken.None);

        // Assert
        var updatedUser = await _dbContext.Users.FindAsync(userId);
        updatedUser.Should().NotBeNull();
        updatedUser!.FullName.Should().Be("Updated Name");
        updatedUser.UserName.Should().Be("original"); // Should not change
        updatedUser.Email.Should().Be("original@test.com"); // Should not change
        
        // Verify validation was NOT called
        _validationHelpersMock.Verify(x => x.EnsureUserNameIsUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<Guid?>()), Times.Never);
        _validationHelpersMock.Verify(x => x.EnsureEmailIsUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<Guid?>()), Times.Never);
    }
}
