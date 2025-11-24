using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs;
using Application.DTOs.Analytics;
using Application.DTOs.Auth;
using FluentAssertions;
using WebApi.IntegrationTests.Common;
using Xunit;

namespace WebApi.IntegrationTests.Controllers;

public class AnalyticsControllerTests : BaseIntegrationTest
{
    public AnalyticsControllerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Overview_ShouldReturn401Unauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await Client.GetAsync("/api/analytics/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Overview_ShouldReturn200OK_WithStats()
    {
        // Arrange
        var (token, _) = await SetupUserWithDataAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var from = DateTime.UtcNow.AddDays(-10).ToString("o");
        var to = DateTime.UtcNow.AddDays(10).ToString("o");

        // Act
        var response = await Client.GetAsync($"/api/analytics/overview?from={from}&to={to}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<OverviewDto>();
        result.Should().NotBeNull();
        result!.TotalVolume.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Overview_ShouldReturn400BadRequest_WhenDateFormatInvalid()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/analytics/overview?from=invalid&to=invalid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Volume_ShouldReturn200OK_WhenGroupByDay()
    {
        // Arrange
        var (token, _) = await SetupUserWithDataAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var from = DateTime.UtcNow.AddDays(-10).ToString("o");
        var to = DateTime.UtcNow.AddDays(10).ToString("o");

        // Act
        var response = await Client.GetAsync($"/api/analytics/volume?from={from}&to={to}&groupBy=day");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TimePointDto>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Volume_ShouldReturn200OK_WhenGroupByWeek()
    {
        // Arrange
        var (token, _) = await SetupUserWithDataAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var from = DateTime.UtcNow.AddDays(-10).ToString("o");
        var to = DateTime.UtcNow.AddDays(10).ToString("o");

        // Act
        var response = await Client.GetAsync($"/api/analytics/volume?from={from}&to={to}&groupBy=week");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TimePointDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Volume_ShouldReturn400BadRequest_WhenGroupByInvalid()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var from = DateTime.UtcNow.AddDays(-10).ToString("o");
        var to = DateTime.UtcNow.AddDays(10).ToString("o");

        // Act
        var response = await Client.GetAsync($"/api/analytics/volume?from={from}&to={to}&groupBy=invalid_group");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Volume_ShouldReturn200OK_WhenGroupByExercise()
    {
        // Arrange
        var (token, _) = await SetupUserWithDataAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var from = DateTime.UtcNow.AddDays(-10).ToString("o");
        var to = DateTime.UtcNow.AddDays(10).ToString("o");

        // Act
        var response = await Client.GetAsync($"/api/analytics/volume?from={from}&to={to}&groupBy=exercise");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TimePointDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task E1rm_ShouldReturn200OK_WithHistory()
    {
        // Arrange
        var (token, _) = await SetupUserWithDataAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var from = DateTime.UtcNow.AddDays(-10).ToString("o");
        var to = DateTime.UtcNow.AddDays(10).ToString("o");

        // Act
        var response = await Client.GetAsync($"/api/analytics/exercises/Bench%20Press/e1rm?from={from}&to={to}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<E1rmPointDto>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task E1rm_ShouldReturn400BadRequest_WhenExerciseNameEmpty()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var from = DateTime.UtcNow.AddDays(-10).ToString("o");
        var to = DateTime.UtcNow.AddDays(10).ToString("o");

        // Act
        var response = await Client.GetAsync($"/api/analytics/exercises/%20/e1rm?from={from}&to={to}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Adherence_ShouldReturn200OK_WithStats()
    {
        // Arrange
        var (token, _) = await SetupUserWithDataAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var from = DateTime.Today.AddDays(-10).ToString("yyyy-MM-dd");
        var to = DateTime.Today.AddDays(10).ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/analytics/adherence?fromDate={from}&toDate={to}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AdherenceDto>();
        result.Should().NotBeNull();
        result!.Planned.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Adherence_ShouldReturn400BadRequest_WhenDateFormatInvalid()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/analytics/adherence?fromDate=invalid&toDate=invalid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PlanVsActual_ShouldReturn200OK_WhenSessionExists()
    {
        // Arrange
        var (token, session) = await SetupUserWithDataAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync($"/api/analytics/plan-vs-actual?sessionId={session.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<PlanVsActualItemDto>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PlanVsActual_ShouldReturn200OK_EmptyList_WhenSessionNotFound()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync($"/api/analytics/plan-vs-actual?sessionId={Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<PlanVsActualItemDto>>();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PlanVsActual_ShouldReturn400BadRequest_WhenSessionIdInvalid()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/analytics/plan-vs-actual?sessionId=invalid-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

    private async Task<(string token, WorkoutSessionDto session)> SetupUserWithDataAsync()
    {
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Create Plan
        var createPlanDto = new CreatePlanDto
        {
            PlanName = "Test Plan",
            Type = "PPL",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto
                {
                    Name = "Bench Press",
                    Rest = 120,
                    Sets = new List<SetDto> { new SetDto { Reps = 8, Weight = 100 } }
                }
            }
        };
        var planResponse = await Client.PostAsJsonAsync("/api/plans", createPlanDto);
        var plan = await planResponse.Content.ReadFromJsonAsync<PlanDto>();

        // 2. Schedule Workout
        var createScheduledDto = new CreateScheduledDto
        {
            PlanId = plan!.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = "planned"
        };
        var scheduledResponse = await Client.PostAsJsonAsync("/api/scheduled", createScheduledDto);
        var scheduled = await scheduledResponse.Content.ReadFromJsonAsync<ScheduledDto>();

        // 3. Start Session
        var startRequest = new StartSessionRequest { ScheduledId = scheduled!.Id };
        var sessionResponse = await Client.PostAsJsonAsync("/api/sessions/start", startRequest);
        var session = await sessionResponse.Content.ReadFromJsonAsync<WorkoutSessionDto>();

        // 3.5 Patch Set (Log data)
        var exercise = session!.Exercises.First();
        var set = exercise.Sets.First();
        var patchRequest = new PatchSetRequest
        {
            RepsDone = 10,
            WeightDone = 100,
            Rpe = 8,
            IsFailure = false
        };
        await Client.PatchAsJsonAsync($"/api/sessions/{session.Id}/sets/{set.Id}", patchRequest);

        // 4. Complete Session (to have data for analytics)
        var completeRequest = new CompleteSessionRequest { CompletedAtUtc = DateTime.UtcNow };
        await Client.PostAsJsonAsync($"/api/sessions/{session.Id}/complete", completeRequest);

        return (token, session);
    }
}
