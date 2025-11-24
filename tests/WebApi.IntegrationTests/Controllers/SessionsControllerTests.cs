using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs;
using Application.DTOs.Auth;
using FluentAssertions;
using WebApi.IntegrationTests.Common;
using Xunit;

namespace WebApi.IntegrationTests.Controllers;

public class SessionsControllerTests : BaseIntegrationTest
{
    public SessionsControllerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Start_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await Client.PostAsync("/api/sessions/start", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Start_ShouldReturn201Created_WhenValidScheduledWorkout()
    {
        // Arrange
        var (token, scheduled) = await SetupAuthenticatedUserWithScheduledAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var startRequest = new StartSessionRequest { ScheduledId = scheduled.Id };

        // Act
        var response = await Client.PostAsJsonAsync("/api/sessions/start", startRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<WorkoutSessionDto>();
        result.Should().NotBeNull();
        result!.ScheduledId.Should().Be(scheduled.Id);
        result.Status.Should().Be("inprogress");
        result.Exercises.Should().HaveCount(1);
    }

    [Fact]
    public async Task Start_ShouldReturn404NotFound_WhenScheduledNotFound()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var startRequest = new StartSessionRequest { ScheduledId = Guid.NewGuid() };

        // Act
        var response = await Client.PostAsJsonAsync("/api/sessions/start", startRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PatchSet_ShouldReturn200OK_AndUpdateSetData()
    {
        // Arrange
        var (token, session) = await StartSessionAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var exerciseId = session.Exercises.First().Id;
        var setId = session.Exercises.First().Sets.First().Id;

        var patchRequest = new PatchSetRequest
        {
            RepsDone = 10,
            WeightDone = 105,
            Rpe = 8,
            IsFailure = false
        };

        // Act
        var response = await Client.PatchAsJsonAsync(
            $"/api/sessions/{session.Id}/sets/{setId}", 
            patchRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WorkoutSessionDto>();
        result.Should().NotBeNull();
        var updatedSet = result!.Exercises.First().Sets.First();
        updatedSet.RepsDone.Should().Be(10);
        updatedSet.WeightDone.Should().Be(105);
    }

    [Fact]
    public async Task AddExercise_ShouldReturn200OK_AndAddAdHocExercise()
    {
        // Arrange
        var (token, session) = await StartSessionAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var addExerciseRequest = new AddSessionExerciseRequest
        {
            Name = "Extra Bicep Curls",
            RestSecPlanned = 60,
            Sets = new List<AddSessionSetRequest>
            {
                new AddSessionSetRequest { RepsPlanned = 12, WeightPlanned = 20 }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/sessions/{session.Id}/exercises", 
            addExerciseRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WorkoutSessionDto>();
        result.Should().NotBeNull();
        result!.Exercises.Should().HaveCount(2);
        result.Exercises.Last().Name.Should().Be("Extra Bicep Curls");
    }

    [Fact]
    public async Task Complete_ShouldReturn200OK_AndMarkAsCompleted()
    {
        // Arrange
        var (token, session) = await StartSessionAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var completeRequest = new CompleteSessionRequest
        {
            SessionNotes = "Great workout!"
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/sessions/{session.Id}/complete", 
            completeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WorkoutSessionDto>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("completed");
        result.SessionNotes.Should().Be("Great workout!");
        result.DurationSec.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Abort_ShouldReturn200OK_AndMarkAsAborted()
    {
        // Arrange
        var (token, session) = await StartSessionAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var abortRequest = new AbortSessionRequest
        {
            Reason = "Injury"
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/sessions/{session.Id}/abort", 
            abortRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WorkoutSessionDto>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("aborted");
        result.SessionNotes.Should().Contain("Injury");
    }

    [Fact]
    public async Task GetById_ShouldReturn200OK_WhenSessionExists()
    {
        // Arrange
        var (token, session) = await StartSessionAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync($"/api/sessions/{session.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WorkoutSessionDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(session.Id);
    }

    [Fact]
    public async Task GetByRange_ShouldReturn200OK_AndFilterByDateRange()
    {
        // Arrange
        var (token, session) = await StartSessionAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var from = DateTime.UtcNow.AddDays(-1).ToString("o");
        var to = DateTime.UtcNow.AddDays(1).ToString("o");

        // Act
        var response = await Client.GetAsync($"/api/sessions/by-range?fromUtc={from}&toUtc={to}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<WorkoutSessionDto>>();
        result.Should().NotBeNull();
        result!.Should().ContainSingle();
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

    private async Task<(string token, ScheduledDto scheduled)> SetupAuthenticatedUserWithScheduledAsync()
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

        // Create a scheduled workout
        var createScheduledDto = new CreateScheduledDto
        {
            PlanId = plan!.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = "planned"
        };

        var scheduledResponse = await Client.PostAsJsonAsync("/api/scheduled", createScheduledDto);
        var scheduled = await scheduledResponse.Content.ReadFromJsonAsync<ScheduledDto>();

        return (token, scheduled!);
    }

    private async Task<(string token, WorkoutSessionDto session)> StartSessionAsync()
    {
        var (token, scheduled) = await SetupAuthenticatedUserWithScheduledAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var startRequest = new StartSessionRequest { ScheduledId = scheduled.Id };
        var response = await Client.PostAsJsonAsync("/api/sessions/start", startRequest);
        var session = await response.Content.ReadFromJsonAsync<WorkoutSessionDto>();

        return (token, session!);
    }
    [Fact]
    public async Task PatchSet_ShouldReturn400BadRequest_WhenSessionNotInProgress()
    {
        // Arrange
        var (token, session) = await StartSessionAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Complete the session
        await Client.PostAsJsonAsync($"/api/sessions/{session.Id}/complete", new CompleteSessionRequest());

        var exerciseId = session.Exercises.First().Id;
        var setId = session.Exercises.First().Sets.First().Id;

        var patchRequest = new PatchSetRequest { RepsDone = 10 };

        // Act
        var response = await Client.PatchAsJsonAsync(
            $"/api/sessions/{session.Id}/sets/{setId}", 
            patchRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PatchSet_ShouldReturn404NotFound_WhenSessionNotFound()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var patchRequest = new PatchSetRequest { RepsDone = 10 };

        // Act
        var response = await Client.PatchAsJsonAsync(
            $"/api/sessions/{Guid.NewGuid()}/sets/{Guid.NewGuid()}", 
            patchRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddExercise_ShouldReturn400BadRequest_WhenSessionNotInProgress()
    {
        // Arrange
        var (token, session) = await StartSessionAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Complete the session
        await Client.PostAsJsonAsync($"/api/sessions/{session.Id}/complete", new CompleteSessionRequest());

        var addExerciseRequest = new AddSessionExerciseRequest
        {
            Name = "Extra",
            Sets = new List<AddSessionSetRequest>
            {
                new AddSessionSetRequest { RepsPlanned = 10, WeightPlanned = 50 }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/sessions/{session.Id}/exercises", 
            addExerciseRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddExercise_ShouldReturn404NotFound_WhenSessionNotFound()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var addExerciseRequest = new AddSessionExerciseRequest
        {
            Name = "Extra",
            Sets = new List<AddSessionSetRequest>
            {
                new AddSessionSetRequest { RepsPlanned = 10, WeightPlanned = 50 }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/sessions/{Guid.NewGuid()}/exercises", 
            addExerciseRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Complete_ShouldReturn400BadRequest_WhenSessionNotInProgress()
    {
        // Arrange
        var (token, session) = await StartSessionAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Complete the session first time
        await Client.PostAsJsonAsync($"/api/sessions/{session.Id}/complete", new CompleteSessionRequest());

        // Act - Try to complete again
        var response = await Client.PostAsJsonAsync(
            $"/api/sessions/{session.Id}/complete", 
            new CompleteSessionRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Complete_ShouldReturn404NotFound_WhenSessionNotFound()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/sessions/{Guid.NewGuid()}/complete", 
            new CompleteSessionRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Abort_ShouldReturn400BadRequest_WhenSessionNotInProgress()
    {
        // Arrange
        var (token, session) = await StartSessionAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Complete the session
        await Client.PostAsJsonAsync($"/api/sessions/{session.Id}/complete", new CompleteSessionRequest());

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/sessions/{session.Id}/abort", 
            new AbortSessionRequest { Reason = "Test" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Abort_ShouldReturn404NotFound_WhenSessionNotFound()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/sessions/{Guid.NewGuid()}/abort", 
            new AbortSessionRequest { Reason = "Test" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ShouldReturn404NotFound_WhenSessionNotFound()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync($"/api/sessions/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
