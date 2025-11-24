using Application.Abstractions;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Application.UnitTests.Services;

public class UserAdminServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly UserAdminService _sut;

    public UserAdminServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _passwordHasherMock = new Mock<IPasswordHasher>();
        
        _sut = new UserAdminService(_dbContext, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldReturnOk_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        
        var user = new User { Id = userId, UserName = "user", Email = "user@test.com", FullName = "User", PasswordHash = "hash" };
        var role = new Role { Id = roleId, Name = "User" };
        
        await _dbContext.Users.AddAsync(user);
        await _dbContext.Roles.AddAsync(role);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.AssignRoleAsync(userId, "User", CancellationToken.None);

        // Assert
        result.Should().Be(AssignRoleResult.Ok);
        var userRole = await _dbContext.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        userRole.Should().NotBeNull();
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        
        // Act
        var result = await _sut.AssignRoleAsync(userId, "User", CancellationToken.None);

        // Assert
        result.Should().Be(AssignRoleResult.UserNotFound);
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldReturnRoleNotFound_WhenRoleDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "user", Email = "user@test.com", FullName = "User", PasswordHash = "hash" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.AssignRoleAsync(userId, "NonExistentRole", CancellationToken.None);

        // Assert
        result.Should().Be(AssignRoleResult.RoleNotFound);
    }

    [Fact]
    public async Task AssignRoleAsync_ShouldReturnAlreadyHasRole_WhenUserAlreadyHasRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        
        var user = new User { Id = userId, UserName = "user", Email = "user@test.com", FullName = "User", PasswordHash = "hash" };
        var role = new Role { Id = roleId, Name = "User" };
        var userRole = new UserRole { UserId = userId, RoleId = roleId };
        
        await _dbContext.Users.AddAsync(user);
        await _dbContext.Roles.AddAsync(role);
        await _dbContext.UserRoles.AddAsync(userRole);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.AssignRoleAsync(userId, "User", CancellationToken.None);

        // Assert
        result.Should().Be(AssignRoleResult.AlreadyHasRole);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnOk_WhenUserExistsAndNotAdmin()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "user", Email = "user@test.com", FullName = "User", PasswordHash = "hash" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteUserAsync(userId, CancellationToken.None);

        // Assert
        result.Should().Be(DeleteUserResult.Ok);
        var deletedUser = await _dbContext.Users.FindAsync(userId);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _sut.DeleteUserAsync(userId, CancellationToken.None);

        // Assert
        result.Should().Be(DeleteUserResult.NotFound);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnIsAdmin_WhenUserIsAdmin()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var adminRoleId = Guid.NewGuid();
        
        var user = new User { Id = userId, UserName = "admin", Email = "admin@test.com", FullName = "Admin", PasswordHash = "hash" };
        var adminRole = new Role { Id = adminRoleId, Name = "Admin" };
        var userRole = new UserRole { UserId = userId, RoleId = adminRoleId, User = user, Role = adminRole };
        
        user.UserRoles.Add(userRole);
        
        await _dbContext.Users.AddAsync(user);
        await _dbContext.Roles.AddAsync(adminRole);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteUserAsync(userId, CancellationToken.None);

        // Assert
        result.Should().Be(DeleteUserResult.IsAdmin);
        var userStillExists = await _dbContext.Users.FindAsync(userId);
        userStillExists.Should().NotBeNull(); // Should not be deleted
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturnOk_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "user", Email = "user@test.com", FullName = "User", PasswordHash = "oldhash" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        _passwordHasherMock.Setup(x => x.HashPassword("newpassword"))
            .Returns("newhash");

        // Act
        var result = await _sut.ResetPasswordAsync(userId, "newpassword", CancellationToken.None);

        // Assert
        result.Should().Be(ResetPasswordResult.Ok);
        var updatedUser = await _dbContext.Users.FindAsync(userId);
        updatedUser!.PasswordHash.Should().Be("newhash");
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _sut.ResetPasswordAsync(userId, "newpassword", CancellationToken.None);

        // Assert
        result.Should().Be(ResetPasswordResult.NotFound);
    }
}
