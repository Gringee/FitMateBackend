using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Controllers;
using Xunit;

namespace WebApi.Tests;

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryService> _svc = new(MockBehavior.Strict);
    private readonly CategoriesController _ctrl;

    public CategoriesControllerTests() => _ctrl = new(_svc.Object);

    [Fact]
    public async Task Create_ReturnsCreated()
    {
        var inDto = new CreateCategoryDto { Name = "Strength" };
        var outDto = new CategoryDto { CategoryId = Guid.NewGuid(), Name = "Strength" };
        _svc.Setup(s => s.CreateAsync(inDto)).ReturnsAsync(outDto);

        var res = await _ctrl.Create(inDto);
        var created = Assert.IsType<CreatedAtActionResult>(res.Result);

        Assert.Equal(outDto, created.Value);
    }
}
