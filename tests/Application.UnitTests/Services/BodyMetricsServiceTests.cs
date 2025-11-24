using Application.Abstractions;
using Application.DTOs.BodyMetrics;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Application.UnitTests.Services;

public class BodyMetricsServiceTests
{
    private readonly Mock<IApplicationDbContext> _dbMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly BodyMetricsService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public BodyMetricsServiceTests()
    {
        _dbMock = new Mock<IApplicationDbContext>();
        _currentUserMock = new Mock<ICurrentUserService>();
        _currentUserMock.Setup(x => x.UserId).Returns(_userId);

        _service = new BodyMetricsService(_dbMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task AddMeasurementAsync_ShouldCalculateBMI_Correctly()
    {
        // Arrange
        var dto = new CreateBodyMeasurementDto(
            WeightKg: 80,
            HeightCm: 180,
            BodyFatPercentage: null,
            ChestCm: null,
            WaistCm: null,
            HipsCm: null,
            BicepsCm: null,
            ThighsCm: null,
            Notes: null
        );

        var dbSetMock = new Mock<DbSet<BodyMeasurement>>();
        _dbMock.Setup(x => x.BodyMeasurements).Returns(dbSetMock.Object);

        // Act
        var result = await _service.AddMeasurementAsync(dto);

        // Assert
        // BMI = 80 / (1.8 * 1.8) = 80 / 3.24 = 24.6913...
        result.BMI.Should().Be(24.69m);
        result.WeightKg.Should().Be(80);
        result.HeightCm.Should().Be(180);
        
        dbSetMock.Verify(x => x.Add(It.IsAny<BodyMeasurement>()), Times.Once);
        _dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddMeasurementAsync_ShouldCalculateBMI_Correctly_ForOverweight()
    {
        // Arrange
        var dto = new CreateBodyMeasurementDto(
            WeightKg: 100,
            HeightCm: 180,
            BodyFatPercentage: null,
            ChestCm: null,
            WaistCm: null,
            HipsCm: null,
            BicepsCm: null,
            ThighsCm: null,
            Notes: null
        );

        var dbSetMock = new Mock<DbSet<BodyMeasurement>>();
        _dbMock.Setup(x => x.BodyMeasurements).Returns(dbSetMock.Object);

        // Act
        var result = await _service.AddMeasurementAsync(dto);

        // Assert
        // BMI = 100 / (1.8 * 1.8) = 100 / 3.24 = 30.864...
        result.BMI.Should().Be(30.86m);
    }
}
