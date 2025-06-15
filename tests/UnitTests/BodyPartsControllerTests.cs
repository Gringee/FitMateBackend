using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Controllers;
using Xunit;

namespace WebApi.Tests;

public class BodyPartsControllerTests
{
    private readonly Mock<IBodyPartService> _svc = new(MockBehavior.Strict);
    private readonly BodyPartsController _ctrl;

    public BodyPartsControllerTests() => _ctrl = new(_svc.Object);

    [Fact]
    public async Task GetAll_ReturnsList()
    {
        var list = new List<BodyPartDto> { new() { BodyPartId = Guid.NewGuid(), Name = "Chest" } };
        _svc.Setup(s => s.GetAllAsync()).ReturnsAsync(list);

        var res = await _ctrl.GetAll();
        var ok = Assert.IsType<OkObjectResult>(res.Result);

        Assert.Equal(list, ok.Value);
    }

    [Fact]
    public async Task Get_NotFound()
    {
        _svc.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BodyPartDto?)null);
        var res = await _ctrl.Get(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(res.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreated()
    {
        var inDto = new CreateBodyPartDto { Name = "Back" };
        var outDto = new BodyPartDto { BodyPartId = Guid.NewGuid(), Name = "Back" };

        _svc.Setup(s => s.CreateAsync(inDto)).ReturnsAsync(outDto);
        var res = await _ctrl.Create(inDto);

        var created = Assert.IsType<CreatedAtActionResult>(res.Result);
        Assert.Equal(outDto, created.Value);
    }
}
