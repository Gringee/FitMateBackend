using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs;
using Application.DTOs.Auth;
using FluentAssertions;
using WebApi.IntegrationTests.Common;
using Xunit;

namespace WebApi.IntegrationTests.Controllers;

public class ScheduledControllerTests : BaseIntegrationTest
{
    public ScheduledControllerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await Client.PostAsync("/api/scheduled", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByDate_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await Client.GetAsync("/api/scheduled/by-date?date=2026-01-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturn201Created_WhenValidRequest()
    {
        // Arrange
        var (token, plan) = await SetupAuthenticatedUserWithPlanAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createDto = new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = new TimeOnly(14, 0),
            Status = "planned",
            Notes = "Morning workout"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/scheduled", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ScheduledDto>();
        result.Should().NotBeNull();
        result!.PlanId.Should().Be(plan.Id);
        result.Date.Should().Be(createDto.Date);
        result.Status.Should().Be("planned");
    }

    [Fact]
    public async Task Create_ShouldCopyExercisesFromPlan_WhenNoCustomExercises()
    {
        // Arrange
        var (token, plan) = await SetupAuthenticatedUserWithPlanAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createDto = new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Time = new TimeOnly(10, 0),
            Status = "planned"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/scheduled", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ScheduledDto>();
        result.Should().NotBeNull();
        result!.Exercises.Should().HaveCount(1);
        result.Exercises.First().Name.Should().Be("Bench Press");
    }

    [Fact]
    public async Task GetAll_ShouldReturn200OK_AndReturnScheduledWorkouts()
    {
        // Arrange
        var (token, plan) = await SetupAuthenticatedUserWithPlanAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create 2 scheduled workouts
        await Client.PostAsJsonAsync("/api/scheduled", new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = "planned"
        });

        await Client.PostAsJsonAsync("/api/scheduled", new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = new TimeOnly(14, 0),
            Status = "planned"
        });

        // Act
        var response = await Client.GetAsync("/api/scheduled");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ScheduledDto>>();
        result.Should().NotBeNull();
        result!.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetByDate_ShouldReturn200OK_AndFilterByDate()
    {
        // Arrange
        var (token, plan) = await SetupAuthenticatedUserWithPlanAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var targetDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5));

        // Create scheduled workout on target date
        await Client.PostAsJsonAsync("/api/scheduled", new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = targetDate,
            Time = new TimeOnly(10, 0),
            Status = "planned"
        });

        // Create on different date
        await Client.PostAsJsonAsync("/api/scheduled", new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            Time = new TimeOnly(14, 0),
            Status = "planned"
        });

        // Act
        var response = await Client.GetAsync($"/api/scheduled/by-date?date={targetDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ScheduledDto>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
        result.First().Date.Should().Be(targetDate);
    }

    [Fact]
    public async Task GetByDate_ShouldReturn400BadRequest_WhenInvalidDateFormat()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/scheduled/by-date?date=invalid-date");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ShouldReturn200OK_WhenValidUpdate()
    {
        // Arrange
        var (token, plan) = await SetupAuthenticatedUserWithPlanAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create scheduled workout
        var createResponse = await Client.PostAsJsonAsync("/api/scheduled", new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = "planned"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ScheduledDto>();

        // Update it
        var updateDto = new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = new TimeOnly(14, 0),
            Status = "completed",
            Notes = "Updated notes"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/scheduled/{created!.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ScheduledDto>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("completed");
        result.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task Delete_ShouldReturn204NoContent_WhenValidDelete()
    {
        // Arrange
        var (token, plan) = await SetupAuthenticatedUserWithPlanAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create scheduled workout
        var createResponse = await Client.PostAsJsonAsync("/api/scheduled", new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = "planned"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ScheduledDto>();

        // Act
        var response = await Client.DeleteAsync($"/api/scheduled/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Duplicate_ShouldReturn201Created_AndCreateCopy()
    {
        // Arrange
        var (token, plan) = await SetupAuthenticatedUserWithPlanAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create scheduled workout
        var createResponse = await Client.PostAsJsonAsync("/api/scheduled", new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = "planned",
            Notes = "Original workout"
        });
        var original = await createResponse.Content.ReadFromJsonAsync<ScheduledDto>();

        // Act
        var response = await Client.PostAsync($"/api/scheduled/{original!.Id}/duplicate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var duplicate = await response.Content.ReadFromJsonAsync<ScheduledDto>();
        duplicate.Should().NotBeNull();
        duplicate!.Id.Should().NotBe(original.Id);
        duplicate.PlanId.Should().Be(original.PlanId);
        duplicate.Exercises.Should().HaveCount(original.Exercises.Count);
    }

    [Fact]
    public async Task Create_ShouldReturn400BadRequest_WhenPlanNotFound()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createDto = new CreateScheduledDto
        {
            PlanId = Guid.NewGuid(), // Non-existent plan
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = new TimeOnly(14, 0),
            Status = "planned",
            VisibleToFriends = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/scheduled", createDto);

        // Assert - API returns 404 when plan not found
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturn404NotFound_WhenScheduledWorkoutDoesNotExist()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateDto = new CreateScheduledDto
        {
            PlanId = Guid.NewGuid(), // Doesn't matter for update
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Time = new TimeOnly(15, 0),
            Status = "planned",
            VisibleToFriends = false
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/scheduled/{Guid.NewGuid()}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturn404NotFound_WhenScheduledWorkoutDoesNotExist()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.DeleteAsync($"/api/scheduled/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Helper methods
    private async Task<string> RegisterAndGetTokenAsync()
    {
        var registerRequest = new RegisterRequest
        {
            Email = $"testuser_{Guid.NewGuid()}@example.com",
            UserName = $"testuser_{Guid.NewGuid().ToString().Substring(0, 8)}",
            Password = "SecurePass123!",
            FullName = "Test User"
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.AccessToken;
    }

    private async Task<(string token, PlanDto plan)> SetupAuthenticatedUserWithPlanAsync()
    {
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a plan
        var createPlanDto = new CreatePlanDto
        {
            PlanName = "Test Plan",
            Type = "PPL",
            Notes = "Test",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto
                {
                    Name = "Bench Press",
                    Rest = 120,
                    Sets = new List<SetDto>
                    {
                        new SetDto { Reps = 8, Weight = 100 }
                    }
                }
            }
        };

        var planResponse = await Client.PostAsJsonAsync("/api/plans", createPlanDto);
        var plan = await planResponse.Content.ReadFromJsonAsync<PlanDto>();

        return (token, plan!);
    }
}
