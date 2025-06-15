using Application.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Controllers;
using Xunit;

namespace WebApi.Tests;

public class AuthControllerTests
{
    private readonly Mock<AuthService> _mockSvc = new(MockBehavior.Strict);
    private readonly AuthController _ctrl;

    public AuthControllerTests()
    {
        _ctrl = new AuthController(_mockSvc.Object);
    }

    [Fact]
    public async Task Register_Success_ReturnsOk()
    {
        var req = new RegisterRequest("user", "u@e.pl", "Pass123");
        _mockSvc.Setup(s => s.RegisterAsync(req.Username, req.Email, req.Password))
                .ReturnsAsync((true, null));

        var res = await _ctrl.Register(req);

        var ok = Assert.IsType<OkObjectResult>(res);
        Assert.Equal("Registered successfully.", ok.Value);
    }

    [Fact]
    public async Task Register_Fail_ReturnsBadRequest()
    {
        _mockSvc.Setup(s => s.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((false, "exists"));

        var res = await _ctrl.Register(new("u", "e", "p"));

        var bad = Assert.IsType<BadRequestObjectResult>(res);
        Assert.Equal("exists", bad.Value);
    }

    [Fact]
    public async Task Login_Ok_ReturnsToken()
    {
        _mockSvc.Setup(s => s.LoginAsync("e", "p")).ReturnsAsync("jwt");
        var res = await _ctrl.Login(new("e", "p"));
        var ok = Assert.IsType<OkObjectResult>(res);

        Assert.Equal("jwt", ((dynamic)ok.Value).token);
    }

    [Fact]
    public async Task Login_Wrong_ReturnsUnauthorized()
    {
        _mockSvc.Setup(s => s.LoginAsync("e", "p")).ReturnsAsync((string?)null);
        var res = await _ctrl.Login(new("e", "p"));
        Assert.IsType<UnauthorizedObjectResult>(res);
    }
}
