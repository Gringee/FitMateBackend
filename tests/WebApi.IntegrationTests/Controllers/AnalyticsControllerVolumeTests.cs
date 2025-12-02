using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs;
using Application.DTOs.Analytics;
using Application.DTOs.Auth;
using FluentAssertions;
using WebApi.IntegrationTests.Common;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi.IntegrationTests.Controllers;

public class AnalyticsControllerVolumeTests : BaseIntegrationTest
{
    private readonly IntegrationTestWebAppFactory _factory;

    public AnalyticsControllerVolumeTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Volume_ShouldReturnMultipleRecords_WhenGroupByDay_AndSessionsOnDifferentDays()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create Plan
        var plan = await CreatePlanAsync(token);

        // Create sessions on different days
        var date1 = DateTime.UtcNow.AddDays(-2);
        var date2 = DateTime.UtcNow.AddDays(-1);
        
        await CreateCompletedSessionAsync(plan.Id, date1);
        await CreateCompletedSessionAsync(plan.Id, date2);

        var from = DateTime.UtcNow.AddDays(-5).ToString("o");
        var to = DateTime.UtcNow.AddDays(1).ToString("o");

        // Act
        var response = await Client.GetAsync($"/api/analytics/volume?from={from}&to={to}&groupBy=day");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TimePointDto>>();
        
        result.Should().NotBeNull();
        result.Should().HaveCount(2, "because we created sessions on 2 different days");
        
        // Verify dates are different
        result!.Select(x => x.Period).Distinct().Should().HaveCount(2);
        
        // Verify format is yyyy-MM-dd
        result.All(x => System.Text.RegularExpressions.Regex.IsMatch(x.Period, @"^\d{4}-\d{2}-\d{2}$"))
            .Should().BeTrue("because Period should be in yyyy-MM-dd format");
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var registerRequest = new RegisterRequest
        {
            Email = $"testuser_vol_{Guid.NewGuid()}@example.com",
            UserName = $"testuser_vol_{Guid.NewGuid().ToString().Substring(0, 8)}",
            Password = "SecurePass123!",
            FullName = "Test User Volume"
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.AccessToken;
    }

    private async Task<PlanDto> CreatePlanAsync(string token)
    {
        var createPlanDto = new CreatePlanDto
        {
            PlanName = "Test Plan Volume",
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
        return (await planResponse.Content.ReadFromJsonAsync<PlanDto>())!;
    }

    private async Task CreateCompletedSessionAsync(Guid planId, DateTime date)
    {
        // Schedule
        var createScheduledDto = new CreateScheduledDto
        {
            PlanId = planId,
            Date = DateOnly.FromDateTime(date),
            Time = new TimeOnly(10, 0),
            Status = "planned"
        };
        var scheduledResponse = await Client.PostAsJsonAsync("/api/scheduled", createScheduledDto);
        var scheduled = await scheduledResponse.Content.ReadFromJsonAsync<ScheduledDto>();

        // Start
        var startRequest = new StartSessionRequest { ScheduledId = scheduled!.Id };
        var sessionResponse = await Client.PostAsJsonAsync("/api/sessions/start", startRequest);
        var session = await sessionResponse.Content.ReadFromJsonAsync<WorkoutSessionDto>();

        // Log data
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

        // Complete
        // IMPORTANT: We need to manually update the StartedAtUtc in the DB or ensure the session creation respects the date.
        // The StartSession endpoint likely sets StartedAtUtc to DateTime.UtcNow.
        // So we might need to hack the DB or use a different approach if we want to simulate past dates.
        // However, looking at the code, StartSession sets StartedAtUtc.
        
        // Let's check StartSession implementation.
        // If StartSession uses DateTime.UtcNow, we can't easily simulate past sessions via API alone without time travel.
        // But wait, the user said "Endpoint ... is failing to group by day".
        // If I run the test now, all sessions will be created "now", so they will fall into the same day.
        // So I need to manually update the StartedAtUtc in the database for the test to work.
        
        // Since this is an integration test with a real DB (Testcontainers), I can't easily access the DbContext directly from the test class 
        // unless I expose it or use a backdoor.
        // But BaseIntegrationTest might expose the factory which exposes the scope.
        
        var completeRequest = new CompleteSessionRequest { CompletedAtUtc = date.AddHours(1) };
        await Client.PostAsJsonAsync($"/api/sessions/{session.Id}/complete", completeRequest);
        
        // Update StartedAtUtc directly in DB to match the requested date
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Application.Abstractions.IApplicationDbContext>();
        var sessionEntity = await db.WorkoutSessions.FindAsync(session.Id);
        if (sessionEntity != null)
        {
            sessionEntity.StartedAtUtc = date;
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Volume_ShouldReturnMultipleRecords_WhenGroupByWeek_AndSessionsOnDifferentWeeks()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create Plan
        var plan = await CreatePlanAsync(token);

        // Create sessions on different weeks
        var date1 = DateTime.UtcNow.AddDays(-21); // 3 weeks ago
        var date2 = DateTime.UtcNow; // Today
        
        await CreateCompletedSessionAsync(plan.Id, date1);
        await CreateCompletedSessionAsync(plan.Id, date2);

        var from = DateTime.UtcNow.AddDays(-30).ToString("o");
        var to = DateTime.UtcNow.AddDays(1).ToString("o");

        // Act
        var response = await Client.GetAsync($"/api/analytics/volume?from={from}&to={to}&groupBy=week");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TimePointDto>>();
        
        result.Should().NotBeNull();
        // We expect at least 2 records if they are in different weeks.
        // Note: AddDays(-21) might be in the same week if we are unlucky with boundaries, but usually 3 weeks diff is enough.
        // To be safe, let's check ISO weeks.
        
        result.Should().HaveCountGreaterThanOrEqualTo(2, "because we created sessions in different weeks");
        
        // Verify periods are different
        result!.Select(x => x.Period).Distinct().Count().Should().BeGreaterThanOrEqualTo(2);
    }
}
