using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Controllers;
using Xunit;

namespace WebApi.Tests
{
    public class FrontendControllerTests
    {
        // Helper: kontroler z poprawnym claimem
        private FrontendController CreateControllerWithUser(Mock<IWorkoutService> svcMock, Guid userId)
        {
            var controller = new FrontendController(svcMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                        }, "test"))
                }
            };
            return controller;
        }

        // Helper: kontroler BEZ clama NameIdentifier
        private FrontendController CreateControllerWithoutUser(Mock<IWorkoutService> svcMock)
        {
            var controller = new FrontendController(svcMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }

        [Fact]
        public async Task GetPlan_ReturnsNotFound_WhenServiceReturnsNull()
        {
            var svcMock = new Mock<IWorkoutService>();
            svcMock.Setup(s => s.GetPlanFrontendAsync(It.IsAny<Guid>()))
                   .ReturnsAsync((FePlanDto?)null);

            var controller = CreateControllerWithoutUser(svcMock);
            var result = await controller.GetPlan(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetPlan_ReturnsOk_WhenServiceReturnsDto()
        {
            var svcMock = new Mock<IWorkoutService>();
            var dto = new FePlanDto {Name = "Plan", Type = "strength", Description = "", Exercises = new List<FeExerciseDto>() };
            svcMock.Setup(s => s.GetPlanFrontendAsync(It.IsAny<Guid>()))
                   .ReturnsAsync(dto);

            var controller = CreateControllerWithoutUser(svcMock);
            var result = await controller.GetPlan(Guid.NewGuid());

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, ok.Value);
        }

        [Fact]
        public async Task SaveScheduled_ReturnsCreatedAtAction_WithCorrectRouteAndValue()
        {
            var svcMock = new Mock<IWorkoutService>();
            var userId = Guid.NewGuid();
            var inputDto = new FeScheduledWorkoutDto
            {
                Date = "2025-06-20",
                Time = "09:00",
                PlanName = "Full Body Workout",
                Exercises = new List<FeExerciseDto>(),
                Status = WorkoutStatus.Planned
            };

            var generatedGuid = Guid.NewGuid();
            var returnedDto = new FeScheduledWorkoutDto
            {
                Id = generatedGuid,
                Date = inputDto.Date,
                Time = inputDto.Time,
                PlanName = inputDto.PlanName,
                Exercises = inputDto.Exercises,
                Status = inputDto.Status
            };

            svcMock.Setup(s => s.SaveScheduledFrontendAsync(
                    It.IsAny<FeScheduledWorkoutDto>(),
                    It.IsAny<Guid>())
                )
                .ReturnsAsync(returnedDto);

            var controller = CreateControllerWithUser(svcMock, userId);

            var actionResult = await controller.SaveScheduled(inputDto);
            var created = Assert.IsType<CreatedAtActionResult>(actionResult);

            Assert.Equal(nameof(FrontendController.GetPlan), created.ActionName);
            Assert.True(created.RouteValues.ContainsKey("id"));
            Assert.Equal(generatedGuid, Guid.Parse(created.RouteValues["id"]!.ToString()));

            var value = Assert.IsType<FeScheduledWorkoutDto>(created.Value);
            Assert.Equal(generatedGuid, value.Id);
            Assert.Equal(inputDto.PlanName, value.PlanName);
            Assert.Equal(WorkoutStatus.Planned, value.Status);
        }

        [Fact]
        public async Task SaveScheduled_CallsServiceWithCorrectParams()
        {
            var svcMock = new Mock<IWorkoutService>();
            var userId = Guid.NewGuid();
            var inputDto = new FeScheduledWorkoutDto
            {
                Date = "2025-07-01",
                Time = "10:30",
                PlanName = "Test Plan",
                Exercises = new List<FeExerciseDto>(),
                Status = WorkoutStatus.Planned
            };

            FeScheduledWorkoutDto? passedDto = null;
            Guid passedUser = Guid.Empty;

            svcMock
                .Setup(s => s.SaveScheduledFrontendAsync(
                    It.IsAny<FeScheduledWorkoutDto>(),
                    It.IsAny<Guid>()))
                .Callback<FeScheduledWorkoutDto, Guid>((dto, uid) =>
                {
                    passedDto = dto;
                    passedUser = uid;
                })
                .ReturnsAsync(inputDto);

            var controller = CreateControllerWithUser(svcMock, userId);
            await controller.SaveScheduled(inputDto);

            svcMock.Verify(s => s.SaveScheduledFrontendAsync(It.IsAny<FeScheduledWorkoutDto>(), It.IsAny<Guid>()), Times.Once);
            Assert.Same(inputDto, passedDto);
            Assert.Equal(userId, passedUser);
        }

        [Fact]
        public async Task SaveScheduled_UsesEmptyGuid_WhenClaimMissing()
        {
            var svcMock = new Mock<IWorkoutService>();
            var inputDto = new FeScheduledWorkoutDto
            {
                Date = "2025-08-01",
                Time = "08:00",
                PlanName = "No Claim",
                Exercises = new List<FeExerciseDto>(),
                Status = WorkoutStatus.Planned
            };

            Guid passedUser = Guid.NewGuid();
            svcMock
                .Setup(s => s.SaveScheduledFrontendAsync(
                    It.IsAny<FeScheduledWorkoutDto>(),
                    It.IsAny<Guid>()))
                .Callback<FeScheduledWorkoutDto, Guid>((_, uid) => passedUser = uid)
                .ReturnsAsync(inputDto);

            var controller = CreateControllerWithoutUser(svcMock);
            await controller.SaveScheduled(inputDto);

            Assert.Equal(Guid.Empty, passedUser);
        }

        [Fact]
        public async Task SaveScheduled_Throws_WhenServiceThrows()
        {
            var svcMock = new Mock<IWorkoutService>();
            var userId = Guid.NewGuid();
            var inputDto = new FeScheduledWorkoutDto
            {
                Date = "2025-09-01",
                Time = "12:00",
                PlanName = "Error Case",
                Exercises = new List<FeExerciseDto>(),
                Status = WorkoutStatus.Planned
            };

            svcMock
                .Setup(s => s.SaveScheduledFrontendAsync(inputDto, userId))
                .ThrowsAsync(new InvalidOperationException("service failure"));

            var controller = CreateControllerWithUser(svcMock, userId);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => controller.SaveScheduled(inputDto));
            Assert.Equal("service failure", ex.Message);
        }
    }
}
