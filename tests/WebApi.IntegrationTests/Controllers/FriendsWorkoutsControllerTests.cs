using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs;
using Application.DTOs.Auth;
using FluentAssertions;
using WebApi.IntegrationTests.Common;
using Xunit;

namespace WebApi.IntegrationTests.Controllers;

public class FriendsWorkoutsControllerTests : BaseIntegrationTest
{
    public FriendsWorkoutsControllerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetFriendsScheduled_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await Client.GetAsync("/api/friends/workouts/scheduled");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFriendsScheduled_ShouldReturn200OK_AndVisibleWorkouts()
    {
        // Arrange
        var senderToken = await RegisterAndGetTokenAsync("sender_fw");
        var recipientToken = await RegisterAndGetTokenAsync("recipient_fw");

        // Establish friendship
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", senderToken);
        await Client.PostAsync("/api/friends/recipient_fw", null);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);
        var incomingResponse = await Client.GetAsync("/api/friends/requests/incoming");
        var requests = await incomingResponse.Content.ReadFromJsonAsync<List<FriendRequestDto>>();
        var requestId = requests!.First().Id;
        await Client.PostAsJsonAsync($"/api/friends/requests/{requestId}/respond", new RespondFriendRequest { Accept = true });

        // Sender creates a visible workout
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", senderToken);
        
        // Create Plan
        var planResponse = await Client.PostAsJsonAsync("/api/plans", new CreatePlanDto 
        { 
            PlanName = "Plan1", 
            Type = "PPL",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto 
                { 
                    Name = "Push Ups", 
                    Rest = 60, 
                    Sets = new List<SetDto> { new SetDto { Reps = 10, Weight = 0 } } 
                }
            }
        });
        planResponse.EnsureSuccessStatusCode();
        var plan = await planResponse.Content.ReadFromJsonAsync<PlanDto>();

        // Create Scheduled Workout (Visible)
        var createResponse = await Client.PostAsJsonAsync("/api/scheduled", new CreateScheduledDto
        {
            PlanId = plan!.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = "planned",
            VisibleToFriends = true
        });
        createResponse.EnsureSuccessStatusCode();

        // Create Scheduled Workout (Not Visible)
        await Client.PostAsJsonAsync("/api/scheduled", new CreateScheduledDto
        {
            PlanId = plan.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Time = new TimeOnly(10, 0),
            Status = "planned",
            VisibleToFriends = false
        });

        // Recipient views workouts
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);

        var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1)).ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/friends/workouts/scheduled?from={today}&to={tomorrow}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var workouts = await response.Content.ReadFromJsonAsync<List<FriendScheduledWorkoutDto>>();
        workouts.Should().NotBeNull();
        workouts!.Should().ContainSingle(w => w.UserName.Contains("sender_fw"));
        workouts.First().Date.Should().Be(DateOnly.FromDateTime(DateTime.Today));
    }

    [Fact]
    public async Task GetFriendsSessions_ShouldReturn401Unauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await Client.GetAsync("/api/friends/workouts/sessions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFriendsSessions_ShouldReturn200OK_AndVisibleSessions()
    {
        // Arrange
        var senderToken = await RegisterAndGetTokenAsync("sender_sess");
        var recipientToken = await RegisterAndGetTokenAsync("recipient_sess");

        // Establish friendship
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", senderToken);
        await Client.PostAsync("/api/friends/recipient_sess", null);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);
        var incomingResponse = await Client.GetAsync("/api/friends/requests/incoming");
        var requests = await incomingResponse.Content.ReadFromJsonAsync<List<FriendRequestDto>>();
        var requestId = requests!.First().Id;
        await Client.PostAsJsonAsync($"/api/friends/requests/{requestId}/respond", new RespondFriendRequest { Accept = true });

        // Sender creates a workout and session
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", senderToken);
        
        var planResponse = await Client.PostAsJsonAsync("/api/plans", new CreatePlanDto 
        { 
            PlanName = "Session Plan", 
            Type = "PPL",
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto 
                { 
                    Name = "Squats", 
                    Rest = 90, 
                    Sets = new List<SetDto> { new SetDto { Reps = 5, Weight = 100 } } 
                }
            }
        });
        var plan = await planResponse.Content.ReadFromJsonAsync<PlanDto>();

        var scheduleResponse = await Client.PostAsJsonAsync("/api/scheduled", new CreateScheduledDto
        {
            PlanId = plan!.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Time = new TimeOnly(10, 0),
            Status = "planned",
            VisibleToFriends = true
        });
        var scheduled = await scheduleResponse.Content.ReadFromJsonAsync<ScheduledDto>();

        // Start and complete session
        var startResponse = await Client.PostAsJsonAsync("/api/sessions/start", new StartSessionRequest { ScheduledId = scheduled!.Id });
        var session = await startResponse.Content.ReadFromJsonAsync<WorkoutSessionDto>();
        await Client.PostAsJsonAsync($"/api/sessions/{session!.Id}/complete", new CompleteSessionRequest { CompletedAtUtc = DateTime.UtcNow });

        // Recipient views sessions
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);
        var fromUtc = DateTime.UtcNow.AddDays(-1).ToString("o");
        var toUtc = DateTime.UtcNow.AddDays(1).ToString("o");

        // Act
        var response = await Client.GetAsync($"/api/friends/workouts/sessions?fromUtc={fromUtc}&toUtc={toUtc}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sessions = await response.Content.ReadFromJsonAsync<List<FriendWorkoutSessionDto>>();
        sessions.Should().NotBeNull();
        sessions!.Should().ContainSingle(s => s.UserName.Contains("sender_sess"));
    }

    [Fact]
    public async Task GetFriendsScheduled_ShouldReturn200OK_EmptyList_WhenNoFriends()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync("loner");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1)).ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/friends/workouts/scheduled?from={today}&to={tomorrow}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var workouts = await response.Content.ReadFromJsonAsync<List<FriendScheduledWorkoutDto>>();
        workouts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFriendsSessions_ShouldReturn200OK_EmptyList_WhenNoFriends()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync("loner2");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var fromUtc = DateTime.UtcNow.AddDays(-1).ToString("o");
        var toUtc = DateTime.UtcNow.AddDays(1).ToString("o");

        // Act
        var response = await Client.GetAsync($"/api/friends/workouts/sessions?fromUtc={fromUtc}&toUtc={toUtc}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sessions = await response.Content.ReadFromJsonAsync<List<FriendWorkoutSessionDto>>();
        sessions.Should().BeEmpty();
    }

    private async Task<string> RegisterAndGetTokenAsync(string usernamePrefix)
    {
        var registerRequest = new RegisterRequest
        {
            Email = $"{usernamePrefix}_{Guid.NewGuid()}@example.com",
            UserName = usernamePrefix,
            Password = "SecurePass123!",
            FullName = "Test User"
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        
        if (!response.IsSuccessStatusCode)
        {
             registerRequest.UserName = $"{usernamePrefix}_{Guid.NewGuid().ToString().Substring(0,5)}";
             response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        }
        
        response.EnsureSuccessStatusCode();
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.AccessToken;
    }
}
