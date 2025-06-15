using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;
using Xunit;

namespace WebApi.Tests;

public class VersionControllerTests
{
    [Fact]
    public void Get_ReturnsVersion()
    {
        var ctrl = new VersionController();
        var res = ctrl.Get();

        var ok = Assert.IsType<OkObjectResult>(res.Result);
        Assert.NotNull(ok.Value);
    }
}
