using System.Security.Claims;
using Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Application.UnitTests.Services;

public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly CurrentUserService _sut;

    public CurrentUserServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _sut = new CurrentUserService(_httpContextAccessorMock.Object);
    }

    [Fact]
    public void UserId_ShouldReturnGuid_WhenUserIsAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _sut.UserId;

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void UserId_ShouldThrowUnauthorizedAccessException_WhenHttpContextIsNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var act = () => _sut.UserId;

        // Assert
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("No HttpContext/User.");
    }

    [Fact]
    public void UserId_ShouldThrowUnauthorizedAccessException_WhenUserIsNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            User = null! // Simulate null user if possible, though DefaultHttpContext initializes it. 
                         // But accessor could return context with null user theoretically? 
                         // Actually DefaultHttpContext.User is never null (returns empty principal).
                         // But let's simulate what happens if GetUserId throws.
        };
        
        // If User is empty principal, GetUserId throws "Missing user id claim."
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var act = () => _sut.UserId;

        // Assert
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("Missing user id claim.");
    }
    
    [Fact]
    public void UserId_ShouldThrowUnauthorizedAccessException_WhenClaimIsMissing()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity()); // No claims
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var act = () => _sut.UserId;

        // Assert
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("Missing user id claim.");
    }
}
