using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Controllers;
using Xunit;

namespace UnitTests;

public class FrontendControllerTests
{
    private static FrontendController ArrangeController(
        Mock<IWorkoutService> svcMock,
        Guid? userIdClaim = null)
    {
        var controller = new FrontendController(svcMock.Object);

        var httpContext = new DefaultHttpContext();
        if (userIdClaim is not null)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userIdClaim.ToString()!) }));
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private static FePlanDto SamplePlan(Guid id) => new()
    {
        Id = id,
        Date = "2025-06-20",
        Time = "18:30",
        Name = "Leg Day",
        Type = "strength",
        Description = "focus on depth"
    };

    private static FeScheduledWorkoutDto SampleScheduled() => new()
    {
        Date = "2025-06-21",
        Time = "07:00",
        PlanName = "Morning Cardio",
        Notes = "Keep HR < 150",
        Status = WorkoutStatus.Planned
    };

    [Fact]
    public async Task GetPlan_returns_200_and_dto_when_service_finds_plan()
    {
        var planId = Guid.NewGuid();
        var dto = SamplePlan(planId);
        var svcMock = new Mock<IWorkoutService>();
        svcMock.Setup(s => s.GetPlanFrontendAsync(planId))
               .ReturnsAsync(dto);

        var controller = ArrangeController(svcMock);

        var result = await controller.GetPlan(planId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(dto);

        svcMock.Verify(s => s.GetPlanFrontendAsync(planId), Times.Once);
    }

    [Fact]
    public async Task GetPlan_returns_404_when_service_returns_null()
    {
        var planId = Guid.NewGuid();
        var svcMock = new Mock<IWorkoutService>();
        svcMock.Setup(s => s.GetPlanFrontendAsync(planId))
               .ReturnsAsync((FePlanDto?)null);

        var controller = ArrangeController(svcMock);

        var result = await controller.GetPlan(planId);

        result.Should().BeOfType<NotFoundResult>();
        svcMock.Verify(s => s.GetPlanFrontendAsync(planId), Times.Once);
    }

    [Fact]
    public async Task SaveScheduled_returns_201_and_location_with_saved_id()
    {
        var userId = Guid.NewGuid();
        var dtoFromBody = SampleScheduled();
        var dtoSaved = new FeScheduledWorkoutDto
        {
            Id = Guid.NewGuid(),
            Date = dtoFromBody.Date,
            Time = dtoFromBody.Time,
            PlanName = dtoFromBody.PlanName,
            Notes = dtoFromBody.Notes,
            Exercises = dtoFromBody.Exercises,
            Status = dtoFromBody.Status
        };

        var svcMock = new Mock<IWorkoutService>();
        svcMock.Setup(s => s.SaveScheduledFrontendAsync(dtoFromBody, userId))
               .ReturnsAsync(dtoSaved);

        var controller = ArrangeController(svcMock, userId);

        var result = await controller.SaveScheduled(dtoFromBody);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(FrontendController.GetPlan));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(dtoSaved.Id);
        created.Value.Should().Be(dtoSaved);
        created.StatusCode.Should().Be(StatusCodes.Status201Created);

        svcMock.Verify(s => s.SaveScheduledFrontendAsync(dtoFromBody, userId), Times.Once);
    }

    [Fact]
    public async Task SaveScheduled_uses_GuidEmpty_when_user_claim_is_missing()
    {
        var dto = SampleScheduled();
        var saved = new FeScheduledWorkoutDto
        {
            Id = Guid.NewGuid(),
            Date = dto.Date,
            Time = dto.Time,
            PlanName = dto.PlanName,
            Notes = dto.Notes,
            Exercises = dto.Exercises,
            Status = dto.Status
        };
        var svcMock = new Mock<IWorkoutService>();
        svcMock.Setup(s => s.SaveScheduledFrontendAsync(dto, Guid.Empty))
               .ReturnsAsync(saved);

        var controller = ArrangeController(svcMock, userIdClaim: null);

        var result = await controller.SaveScheduled(dto);

        result.Should().BeOfType<CreatedAtActionResult>();
        svcMock.Verify(s => s.SaveScheduledFrontendAsync(dto, Guid.Empty), Times.Once);
    }

    [Fact]
    public async Task SaveScheduled_returns_500_when_service_returns_null()
    {
        // arrange
        var userId = Guid.NewGuid();
        var dto = SampleScheduled();
        var svcMock = new Mock<IWorkoutService>();
        svcMock.Setup(s => s.SaveScheduledFrontendAsync(dto, userId))
               .ReturnsAsync((FeScheduledWorkoutDto?)null);

        var controller = ArrangeController(svcMock, userId);

        // act
        var result = await controller.SaveScheduled(dto);

        // assert
        var res = result.Should().BeOfType<StatusCodeResult>().Subject;
        res.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        svcMock.Verify(s => s.SaveScheduledFrontendAsync(dto, userId), Times.Once);
    }

    [Fact]
    public async Task GetPlansForDay_returns_list_for_valid_date()
    {
        // arrange
        var userId = Guid.NewGuid();
        var dateStr = "2025-06-22";
        var dtoList = new List<FePlanDto> { new() { Id = Guid.NewGuid(), Date = dateStr, Name = "Test" } };

        var svcMock = new Mock<IWorkoutService>();
        svcMock.Setup(s => s.GetPlansByDateAsync(userId, DateOnly.Parse(dateStr))).ReturnsAsync(dtoList);

        var ctrl = ArrangeController(svcMock, userId);

        // act
        var result = await ctrl.GetPlansForDay(dateStr);

        // assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dtoList, ok.Value);
    }

    [Fact]
    public async Task GetWorkoutDays_returns_list_from_service()
    {
        // arrange
        var userId = Guid.NewGuid();
        var expected = new List<DayWorkoutRefDto>
    {
        new() { Id = Guid.NewGuid(), Date = "2025-06-22" },
        new() { Id = Guid.NewGuid(), Date = "2025-06-24" }
    };

        var svcMock = new Mock<IWorkoutService>();
        svcMock.Setup(s => s.GetAllWorkoutDaysAsync(userId)).ReturnsAsync(expected);

        var ctrl = ArrangeController(svcMock, userId);

        // act
        var result = await ctrl.GetWorkoutDays();

        // assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }
}
