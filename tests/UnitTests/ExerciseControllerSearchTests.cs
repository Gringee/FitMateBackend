using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Controllers;
using Xunit;

namespace WebApi.Tests;

public class ExerciseControllerSearchTests
{
    private readonly Mock<IExerciseService> _svc = new(MockBehavior.Strict);
    private readonly ExerciseController _ctrl;

    public ExerciseControllerSearchTests() => _ctrl = new(_svc.Object);

    [Fact]
    public async Task Search_ReturnsOk_List()
    {
        var list = new List<ExerciseDto> { new() { Name = "Bench" } };
        _svc.Setup(s => s.SearchAsync("bench")).ReturnsAsync(list);

        var res = await _ctrl.Search("bench");
        var ok = Assert.IsType<OkObjectResult>(res.Result);

        Assert.Equal(list, ok.Value);
    }

    [Fact]
    public async Task Search_EmptyQuery_BadRequest()
    {
        var res = await _ctrl.Search("");
        Assert.IsType<BadRequestObjectResult>(res.Result);
    }
}
