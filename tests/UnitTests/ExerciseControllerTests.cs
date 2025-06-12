using Xunit;
using Moq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Application.DTOs;
using WebApi.Controllers;
using Application.Interfaces;

namespace WebApi.Tests
{
    public class ExerciseControllerTests
    {
        private readonly Mock<IExerciseService> _mockService;
        private readonly ExerciseController _controller;

        public ExerciseControllerTests()
        {
            _mockService = new Mock<IExerciseService>();
            _controller = new ExerciseController(_mockService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task Exercise_Create_ReturnsCreatedAtAction_OnValidDto()
        {
            // Arrange
            var dto = new CreateExerciseDto
            {
                Name = "Push-up",
                DifficultyLevel = "Medium",
                BodyPartId = Guid.NewGuid()
            };
            var created = new ExerciseDto
            {
                ExerciseId = Guid.NewGuid(),
                Name = dto.Name,
                DifficultyLevel = dto.DifficultyLevel,
                BodyPartId = dto.BodyPartId
            };
            _mockService
                .Setup(s => s.CreateAsync(dto))
                .ReturnsAsync(created);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetById), createdResult.ActionName);
            Assert.Equal(created, createdResult.Value);
            Assert.Equal(created.ExerciseId, (Guid)createdResult.RouteValues["id"]);
        }

        [Fact]
        public async Task Exercise_Create_ReturnsBadRequest_OnInvalidModel()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Required");
            var dto = new CreateExerciseDto();

            // Act
            var result = await _controller.Create(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Exercise_GetAll_ReturnsOk_WithList()
        {
            // Arrange
            var list = new List<ExerciseDto> { new ExerciseDto(), new ExerciseDto() };
            _mockService
                .Setup(s => s.GetAllAsync())
                .ReturnsAsync(list);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(list, ok.Value);
        }

        [Fact]
        public async Task Exercise_GetById_ReturnsOk_WhenFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new ExerciseDto { ExerciseId = id };
            _mockService
                .Setup(s => s.GetByIdAsync(id))
                .ReturnsAsync(dto);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, ok.Value);
        }

        [Fact]
        public async Task Exercise_GetById_ReturnsNotFound_WhenNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService
                .Setup(s => s.GetByIdAsync(id))
                .ReturnsAsync((ExerciseDto)null);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            var nf = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains(id.ToString(), nf.Value.ToString());
        }

        [Fact]
        public async Task Exercise_Update_ReturnsBadRequest_OnInvalidModel()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Required");
            var id = Guid.NewGuid();
            var dto = new UpdateExerciseDto();

            // Act
            var result = await _controller.Update(id, dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Exercise_Update_ReturnsNotFound_WhenServiceReturnsFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new UpdateExerciseDto
            {
                Name = "Squat",
                DifficultyLevel = "Easy",
                BodyPartId = Guid.NewGuid()
            };
            _mockService
                .Setup(s => s.UpdateAsync(id, dto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Update(id, dto);

            // Assert
            var nf = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains(id.ToString(), nf.Value.ToString());
        }

        [Fact]
        public async Task Exercise_Update_ReturnsOk_WhenServiceReturnsTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new UpdateExerciseDto
            {
                Name = "Squat",
                DifficultyLevel = "Easy",
                BodyPartId = Guid.NewGuid()
            };
            _mockService
                .Setup(s => s.UpdateAsync(id, dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(id, dto);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Exercise_Delete_ReturnsNoContent_WhenServiceReturnsTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService
                .Setup(s => s.DeleteAsync(id))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Exercise_Delete_ReturnsNotFound_WhenServiceReturnsFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService
                .Setup(s => s.DeleteAsync(id))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            var nf = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains(id.ToString(), nf.Value.ToString());
        }
    }
}
