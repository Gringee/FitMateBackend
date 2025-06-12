using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Application.DTOs;
using Application.Services;
using WebApi.Controllers;
using Application.Interfaces;

namespace WebApi.Tests
{
    public class WorkoutControllerTests
    {
        private readonly Mock<IWorkoutService> _mockService;
        private readonly WorkoutController _controller;

        public WorkoutControllerTests()
        {
            _mockService = new Mock<IWorkoutService>();
            _controller = new WorkoutController(_mockService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        private void SetUserClaim(string type, string value)
        {
            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim(type, value));
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
        }

        [Fact]
        public async Task Workout_CreateWorkout_ReturnsOk_OnSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetUserClaim(ClaimTypes.NameIdentifier, userId.ToString());

            var dto = new CreateWorkoutDto
            {
                WorkoutDate = DateTime.UtcNow,
                DurationMinutes = 30,
                Notes = "Test notes",
                Exercises = new List<WorkoutExerciseDto>()
            };

            _mockService
                .Setup(s => s.CreateWorkoutAsync(userId, dto))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateWorkout(dto);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Workout_CreateWorkout_ReturnsBadRequest_OnArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetUserClaim(ClaimTypes.NameIdentifier, userId.ToString());

            var dto = new CreateWorkoutDto();
            _mockService
                .Setup(s => s.CreateWorkoutAsync(userId, dto))
                .ThrowsAsync(new ArgumentException("Invalid workout data."));

            // Act
            var result = await _controller.CreateWorkout(dto);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid workout data.", bad.Value.ToString());
        }

        [Fact]
        public async Task Workout_GetWorkouts_ReturnsOk_WithWorkouts()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetUserClaim("sub", userId.ToString());

            var workouts = new List<WorkoutDto>
            {
                new WorkoutDto { WorkoutId = Guid.NewGuid(), UserId = userId, DurationMinutes = 20, Exercises = new List<WorkoutExerciseDto>() },
                new WorkoutDto { WorkoutId = Guid.NewGuid(), UserId = userId, DurationMinutes = 45, Exercises = new List<WorkoutExerciseDto>() }
            };

            _mockService
                .Setup(s => s.GetAllForUserAsync(userId))
                .ReturnsAsync(workouts);

            // Act
            var result = await _controller.GetWorkouts();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(workouts, ok.Value);
        }

        [Fact]
        public async Task Workout_DeleteWorkout_ReturnsNoContent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetUserClaim("sub", userId.ToString());

            var workoutId = Guid.NewGuid();
            _mockService
                .Setup(s => s.DeleteWorkoutAsync(userId, workoutId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteWorkout(workoutId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Workout_UpdateWorkout_ReturnsOk_OnSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetUserClaim("sub", userId.ToString());

            var workoutId = Guid.NewGuid();
            var dto = new CreateWorkoutDto
            {
                WorkoutDate = DateTime.UtcNow,
                DurationMinutes = 60,
                Notes = "Updated notes",
                Exercises = new List<WorkoutExerciseDto>()
            };

            _mockService
                .Setup(s => s.UpdateWorkoutAsync(userId, workoutId, dto))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateWorkout(workoutId, dto);

            // Assert
            Assert.IsType<OkResult>(result);
        }
    }
}
