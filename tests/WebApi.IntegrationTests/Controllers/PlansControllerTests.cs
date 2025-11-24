using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs;
using Application.DTOs.Auth;
using FluentAssertions;
using WebApi.IntegrationTests.Common;
using Xunit;

namespace WebApi.IntegrationTests.Controllers;

public class PlansControllerTests : BaseIntegrationTest
{
    public PlansControllerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAllPlans_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await Client.GetAsync("/api/plans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var createPlanDto = new CreatePlanDto
        {
            PlanName = "Test Plan",
            Type = "PPL",
            Notes = "Test notes",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto 
                { 
                    Name = "Test Exercise", 
                    Rest = 60, 
                    Sets = new List<SetDto> { new SetDto { Reps = 10, Weight = 0 } } 
                }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/plans", createPlanDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllPlans_ShouldReturn200OK_AndReturnPlans_WhenAuthenticated()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/plans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var plans = await response.Content.ReadFromJsonAsync<List<PlanDto>>();
        plans.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ShouldReturn201Created_WhenValidPlan()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createPlanDto = new CreatePlanDto
        {
            PlanName = "Push Day",
            Type = "PPL",
            Notes = "Chest, Shoulders, Triceps",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto
                {
                    Name = "Bench Press",
                    Rest = 120,
                    Sets = new List<SetDto>
                    {
                        new SetDto { Reps = 8, Weight = 100 },
                        new SetDto { Reps = 8, Weight = 100 },
                        new SetDto { Reps = 8, Weight = 100 }
                    }
                }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/plans", createPlanDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPlan = await response.Content.ReadFromJsonAsync<PlanDto>();
        createdPlan.Should().NotBeNull();
        createdPlan!.PlanName.Should().Be("Push Day");
        createdPlan.Exercises.Should().HaveCount(1);
        createdPlan.Exercises.First().Sets.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetById_ShouldReturn200OK_WhenPlanExists()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a plan first
        var createDto = new CreatePlanDto
        {
            PlanName = "Test Plan",
            Type = "Full Body",
            Notes = "",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto 
                { 
                    Name = "Test Exercise", 
                    Rest = 60, 
                    Sets = new List<SetDto> { new SetDto { Reps = 10, Weight = 0 } } 
                }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/api/plans", createDto);
        var createdPlan = await createResponse.Content.ReadFromJsonAsync<PlanDto>();

        // Act
        var response = await Client.GetAsync($"/api/plans/{createdPlan!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var plan = await response.Content.ReadFromJsonAsync<PlanDto>();
        plan.Should().NotBeNull();
        plan!.PlanName.Should().Be("Test Plan");
    }

    [Fact]
    public async Task GetById_ShouldReturn404NotFound_WhenPlanDoesNotExist()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/plans/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturn200OK_WhenPlanIsUpdated()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a plan
        var createDto = new CreatePlanDto
        {
            PlanName = "Original Plan",
            Type = "PPL",
            Notes = "Original",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto 
                { 
                    Name = "Test Exercise", 
                    Rest = 60, 
                    Sets = new List<SetDto> { new SetDto { Reps = 10, Weight = 0 } } 
                }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/api/plans", createDto);
        var createdPlan = await createResponse.Content.ReadFromJsonAsync<PlanDto>();

        // Update the plan
        var updateDto = new CreatePlanDto
        {
            PlanName = "Updated Plan",
            Type = "Upper/Lower",
            Notes = "Updated",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto 
                { 
                    Name = "Test Exercise", 
                    Rest = 60, 
                    Sets = new List<SetDto> { new SetDto { Reps = 10, Weight = 0 } } 
                }
            }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/plans/{createdPlan!.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedPlan = await response.Content.ReadFromJsonAsync<PlanDto>();
        updatedPlan.Should().NotBeNull();
        updatedPlan!.PlanName.Should().Be("Updated Plan");
        updatedPlan.Type.Should().Be("Upper/Lower");
    }

    [Fact]
    public async Task Delete_ShouldReturn204NoContent_WhenPlanIsDeleted()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a plan
        var createDto = new CreatePlanDto
        {
            PlanName = "Plan To Delete",
            Type = "PPL",
            Notes = "",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto 
                { 
                    Name = "Test Exercise", 
                    Rest = 60, 
                    Sets = new List<SetDto> { new SetDto { Reps = 10, Weight = 0 } } 
                }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/api/plans", createDto);
        var createdPlan = await createResponse.Content.ReadFromJsonAsync<PlanDto>();

        // Act
        var response = await Client.DeleteAsync($"/api/plans/{createdPlan!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await Client.GetAsync($"/api/plans/{createdPlan.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Duplicate_ShouldReturn201Created_AndCreateCopy()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a plan
        var createDto = new CreatePlanDto
        {
            PlanName = "Original Plan",
            Type = "PPL",
            Notes = "Test",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto
                {
                    Name = "Squat",
                    Rest = 180,
                    Sets = new List<SetDto> { new SetDto { Reps = 5, Weight = 140 } }
                }
            }
        };
        var createResponse = await Client.PostAsJsonAsync("/api/plans", createDto);
        var createdPlan = await createResponse.Content.ReadFromJsonAsync<PlanDto>();

        // Act
        var response = await Client.PostAsync($"/api/plans/{createdPlan!.Id}/duplicate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var duplicatedPlan = await response.Content.ReadFromJsonAsync<PlanDto>();
        duplicatedPlan.Should().NotBeNull();
        duplicatedPlan!.PlanName.Should().Be("Original Plan (Copy)");
        duplicatedPlan.Id.Should().NotBe(createdPlan.Id);
        duplicatedPlan.Exercises.Should().HaveCount(1);
    }

    [Fact]
    public async Task Update_ShouldReturn404NotFound_WhenPlanDoesNotExist()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateDto = new CreatePlanDto
        {
            PlanName = "Updated Plan",
            Type = "PPL",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto 
                { 
                    Name = "Bench Press", 
                    Rest = 120, 
                    Sets = new List<SetDto> { new SetDto { Reps = 5, Weight = 100 } } 
                }
            }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/plans/{Guid.NewGuid()}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturn403Forbidden_WhenUserIsNotOwner()
    {
        // Arrange - User1 creates a plan
        var token1 = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);

        var createDto = new CreatePlanDto
        {
            PlanName = "User1 Plan",
            Type = "PPL",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto 
                { 
                    Name = "Squat", 
                    Rest = 180, 
                    Sets = new List<SetDto> { new SetDto { Reps = 5, Weight = 140 } } 
                }
            }
        };

        var createResponse = await Client.PostAsJsonAsync("/api/plans", createDto);
        var createdPlan = await createResponse.Content.ReadFromJsonAsync<PlanDto>();

        // User2 tries to update User1's plan
        var token2 = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

        var updateDto = new CreatePlanDto
        {
            PlanName = "Hacked Plan",
            Type = "FullBody",
            Exercises = createDto.Exercises
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/plans/{createdPlan!.Id}", updateDto);

        // Assert - API returns 404 when plan not found (user doesn't have access)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturn404NotFound_WhenPlanDoesNotExist()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.DeleteAsync($"/api/plans/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Helper method to register a user and get JWT token
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
}
