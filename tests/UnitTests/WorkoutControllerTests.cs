using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Application.DTOs;
using Application.Interfaces;
using WebApi.Controllers;
using Domain.Enums;

namespace WebApi.Tests;

public class WorkoutControllerTests
{
    private readonly Mock<IWorkoutService> _svc;
    private readonly WorkoutController _ctrl;
    private readonly Guid _userId;

    public WorkoutControllerTests()
    {
        _svc = new Mock<IWorkoutService>(MockBehavior.Strict);
        _ctrl = new WorkoutController(_svc.Object)
        {
            // wspólne „fałszywe” uwierzytelnienie
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        _userId = Guid.NewGuid();
        SetUserClaim(ClaimTypes.NameIdentifier, _userId.ToString());
    }

    /* ----------------- helper ----------------- */
    private void SetUserClaim(string type, string value)
    {
        var id = new ClaimsIdentity();
        id.AddClaim(new Claim(type, value));
        _ctrl.ControllerContext.HttpContext.User = new ClaimsPrincipal(id);
    }

    /* ----------------- CREATE ----------------- */

    [Fact]
    public async Task CreateWorkout_ReturnsOk()
    {
        var dto = new CreateWorkoutDto
        {
            WorkoutDate = DateTime.UtcNow,
            DurationMinutes = 30,
            Notes = "Test",
            Exercises = new()
        };

        _svc.Setup(s => s.CreateWorkoutAsync(_userId, dto))
            .Returns(Task.CompletedTask);

        var result = await _ctrl.CreateWorkout(dto);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task CreateWorkout_BadRequest_OnArgumentException()
    {
        var dto = new CreateWorkoutDto();
        _svc.Setup(s => s.CreateWorkoutAsync(_userId, dto))
            .ThrowsAsync(new ArgumentException("Invalid"));

        var result = await _ctrl.CreateWorkout(dto);
        var bad = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Contains("Invalid", bad.Value!.ToString());
    }

    /* ----------------- LIST ----------------- */

    [Fact]
    public async Task GetWorkouts_ReturnsOkWithList()
    {
        var workouts = new List<WorkoutDto>
        {
            new() { WorkoutId = Guid.NewGuid(), UserId = _userId, DurationMinutes = 20 },
            new() { WorkoutId = Guid.NewGuid(), UserId = _userId, DurationMinutes = 45 }
        };

        _svc.Setup(s => s.GetAllForUserAsync(_userId)).ReturnsAsync(workouts);

        var res = await _ctrl.GetWorkouts();
        var ok = Assert.IsType<OkObjectResult>(res);

        Assert.Equal(workouts, ok.Value);
    }

    /* ----------------- GET by ID ----------------- */

    [Fact]
    public async Task GetById_NotFound()
    {
        _svc.Setup(s => s.GetAllForUserAsync(_userId))
            .ReturnsAsync(new List<WorkoutDto>());     // pusta lista

        var res = await _ctrl.GetById(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(res);
    }

    /* ----------------- UPDATE ----------------- */

    [Fact]
    public async Task UpdateWorkout_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = new CreateWorkoutDto { DurationMinutes = 60 };

        _svc.Setup(s => s.UpdateWorkoutAsync(_userId, id, dto)).Returns(Task.CompletedTask);

        var res = await _ctrl.UpdateWorkout(id, dto);
        Assert.IsType<OkResult>(res);
    }

    /* ----------------- DELETE ----------------- */

    [Fact]
    public async Task DeleteWorkout_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _svc.Setup(s => s.DeleteWorkoutAsync(_userId, id)).Returns(Task.CompletedTask);

        var res = await _ctrl.DeleteWorkout(id);
        Assert.IsType<NoContentResult>(res);
    }

    /* ----------------- DUPLICATE ----------------- */

    [Fact]
    public async Task Duplicate_ReturnsCreated()
    {
        var srcId = Guid.NewGuid();
        var newDto = new WorkoutDto
        {
            WorkoutId = Guid.NewGuid(),
            UserId = _userId,
            DurationMinutes = 30,
            Exercises = new()
        };

        _svc.Setup(s => s.DuplicateAsync(_userId, srcId, It.IsAny<DuplicateWorkoutDto>()))
            .ReturnsAsync(newDto);

        var res = await _ctrl.Duplicate(srcId, new DuplicateWorkoutDto());
        var created = Assert.IsType<CreatedAtActionResult>(res);

        Assert.Equal("GetById", created.ActionName);
        Assert.Equal(newDto, created.Value);
    }

    [Fact]
    public async Task SetStatus_ReturnsNoContent()
    {
        // Arrange
        var workoutId = Guid.NewGuid();

        _svc.Setup(s => s.SetStatusAsync(_userId, workoutId, WorkoutStatus.Completed))
            .ReturnsAsync(true);

        // Act
        var res = await _ctrl.SetStatus(workoutId, WorkoutStatus.Completed);

        // Assert
        Assert.IsType<NoContentResult>(res);
    }
}
