using System;
using System.Linq;
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

public partial class MoreFrontendControllerTests
{
    /* ===== helpery z poprzedniego pliku ===== */

    private static FrontendController ArrangeController(
        Mock<IWorkoutService> svcMock,
        Guid? userIdClaim = null)
    {
        var ctrl = new FrontendController(svcMock.Object);

        var ctx = new DefaultHttpContext();
        if (userIdClaim is not null)
        {
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userIdClaim.ToString()!) }));
        }

        ctrl.ControllerContext = new ControllerContext { HttpContext = ctx };
        return ctrl;
    }

    private static FeScheduledWorkoutDto SampleScheduled() => new()
    {
        Date = "2025-07-01",
        Time = "06:45",
        PlanName = "Mobility",
        Notes = "Light session",
        Status = WorkoutStatus.Planned
    };

    private static FePlanDto SamplePlan(Guid id) => new()
    {
        Id = id,
        Date = "2025-07-02",
        Time = "19:15",
        Name = "Push",
        Type = "strength",
        Description = "Chest & triceps"
    };

    /* --------------------------------------------------------------------- */
    /* 1) Zły ModelState ⇒ 400                                              */
    /* --------------------------------------------------------------------- */

    [Fact]
    public async Task SaveScheduled_returns_400_when_ModelState_is_invalid()
    {
        // arrange
        var svcMock = new Mock<IWorkoutService>();
        var ctrl = ArrangeController(svcMock, Guid.NewGuid());
        ctrl.ModelState.AddModelError("Date", "Required");
        var dto = SampleScheduled();

        // act
        var result = await ctrl.SaveScheduled(dto);

        // assert
        result.Should().BeOfType<BadRequestObjectResult>();
        svcMock.VerifyNoOtherCalls();
    }

    /* --------------------------------------------------------------------- */
    /* 2) Wyjątek z serwisu ⇒ 500 (propagacja)                               */
    /* --------------------------------------------------------------------- */

    [Fact]
    public async Task SaveScheduled_returns_500_when_service_throws()
    {
        // arrange
        var userId = Guid.NewGuid();
        var dto = SampleScheduled();
        var svcMock = new Mock<IWorkoutService>();
        svcMock.Setup(s => s.SaveScheduledFrontendAsync(dto, userId))
               .ThrowsAsync(new InvalidOperationException("DB down"));

        var ctrl = ArrangeController(svcMock, userId);

        // act
        var response = await ctrl.SaveScheduled(dto);

        // assert
        var res = response.Should().BeOfType<ObjectResult>().Subject;
        res.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        svcMock.Verify(s => s.SaveScheduledFrontendAsync(dto, userId), Times.Once);
    }

    /* --------------------------------------------------------------------- */
    /* 3) Atrybut route w GetPlan                                            */
    /* --------------------------------------------------------------------- */

    [Fact]
    public void GetPlan_has_correct_HttpGet_route()
    {
        // arrange
        var method = typeof(FrontendController)
                     .GetMethod(nameof(FrontendController.GetPlan))!;

        // act
        var attr = (HttpGetAttribute)method
                   .GetCustomAttributes(typeof(HttpGetAttribute), false)
                   .Single();

        // assert
        attr.Template.Should().Be("plan/{id:guid}");
    }

    /* --------------------------------------------------------------------- */
    /* 4) Atrybuty POST + kontroler Route                                    */
    /* --------------------------------------------------------------------- */

    [Fact]
    public void Controller_and_SaveScheduled_have_expected_route_attributes()
    {
        // kontroler
        var ctrlRoute = typeof(FrontendController)
                        .GetCustomAttributes(typeof(RouteAttribute), false)
                        .Cast<RouteAttribute>()
                        .Single().Template;
        ctrlRoute.Should().Be("api/frontend");

        // metoda
        var postAttr = (HttpPostAttribute)typeof(FrontendController)
                       .GetMethod(nameof(FrontendController.SaveScheduled))!
                       .GetCustomAttributes(typeof(HttpPostAttribute), false)
                       .Single();

        postAttr.Template.Should().Be("scheduled");
    }

    /* --------------------------------------------------------------------- */
    /* 5) GetPlan zwraca obiekt FePlanDto                                    */
    /* --------------------------------------------------------------------- */

    [Fact]
    public async Task GetPlan_returns_object_of_expected_type()
    {
        // arrange
        var planId = Guid.NewGuid();
        var dto = SamplePlan(planId);
        var svcMock = new Mock<IWorkoutService>();
        svcMock.Setup(s => s.GetPlanFrontendAsync(planId)).ReturnsAsync(dto);

        var ctrl = ArrangeController(svcMock);

        // act
        var result = await ctrl.GetPlan(planId);

        // assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<FePlanDto>();
    }
}
