using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs;
using Application.DTOs.Auth;
using Application.DTOs.BodyMetrics;
using FluentAssertions;
using WebApi.IntegrationTests.Common;
using Xunit;

namespace WebApi.IntegrationTests.Controllers;

public class BodyMetricsControllerTests : BaseIntegrationTest
{
    public BodyMetricsControllerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task AddMeasurement_ShouldReturn201_WithCalculatedBMI()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new CreateBodyMeasurementDto(
            WeightKg: 82.5m,
            HeightCm: 178,
            BodyFatPercentage: 18.5m,
            ChestCm: 102,
            WaistCm: 85,
            HipsCm: null,
            BicepsCm: null,
            ThighsCm: null,
            Notes: "Test measurement"
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/body-metrics", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var result = await response.Content.ReadFromJsonAsync<BodyMeasurementDto>();
        result.Should().NotBeNull();
        result!.WeightKg.Should().Be(82.5m);
        result.HeightCm.Should().Be(178);
        // BMI = 82.5 / (1.78 * 1.78) = 82.5 / 3.1684 = 26.038...
        result.BMI.Should().Be(26.04m);
        result.Notes.Should().Be("Test measurement");
    }

    [Fact]
    public async Task GetStats_ShouldReturnCorrectStats()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Add first measurement (35 days ago) - simulated by just adding now, 
        // in real scenario we can't easily set CreatedAt without modifying entity/service to allow it or seeding DB directly.
        // For this test we will just add two measurements now.
        
        var dto1 = new CreateBodyMeasurementDto(90, 180, null, null, null, null, null, null, null);
        await Client.PostAsJsonAsync("/api/body-metrics", dto1);

        var dto2 = new CreateBodyMeasurementDto(85, 180, null, null, null, null, null, null, null);
        await Client.PostAsJsonAsync("/api/body-metrics", dto2);

        // Act
        var response = await Client.GetAsync("/api/body-metrics/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<BodyMetricsStatsDto>();
        
        stats.Should().NotBeNull();
        stats!.CurrentWeightKg.Should().Be(85);
        stats.HighestWeight.Should().Be(90);
        stats.LowestWeight.Should().Be(85);
        stats.TotalMeasurements.Should().Be(2);
        // BMI = 85 / (1.8*1.8) = 26.23
        stats.CurrentBMI.Should().Be(26.23m);
        stats.BMICategory.Should().Be("Overweight");
    }

    [Fact]
    public async Task GetProgress_ShouldReturnMeasurementsInOrder()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var dto1 = new CreateBodyMeasurementDto(90, 180, null, null, null, null, null, null, null);
        await Client.PostAsJsonAsync("/api/body-metrics", dto1);
        
        var dto2 = new CreateBodyMeasurementDto(88, 180, null, null, null, null, null, null, null);
        await Client.PostAsJsonAsync("/api/body-metrics", dto2);

        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddDays(1);

        // Act
        var response = await Client.GetAsync($"/api/body-metrics/progress?from={from:O}&to={to:O}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var progress = await response.Content.ReadFromJsonAsync<List<BodyMetricsProgressDto>>();
        
        progress.Should().HaveCount(2);
        progress![0].WeightKg.Should().Be(90);
        progress[1].WeightKg.Should().Be(88);
    }

    [Fact]
    public async Task GetFriendMetrics_ShouldReturnMetrics_WhenFriendSharesBiometrics()
    {
        // Arrange
        var user1Token = await RegisterAndGetTokenAsync();
        var user2Token = await RegisterAndGetTokenAsync();
        
        // User 1 enables sharing
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);
        await Client.PutAsJsonAsync("/api/userprofile/biometrics-privacy", new { ShareWithFriends = true });
        
        // User 1 adds measurement
        var dto = new CreateBodyMeasurementDto(80, 180, null, null, null, null, null, null, null);
        await Client.PostAsJsonAsync("/api/body-metrics", dto);

        // Get User 1 ID
        var profileResponse = await Client.GetAsync("/api/userprofile");
        var profile = await profileResponse.Content.ReadFromJsonAsync<UserProfileDto>();
        var user1Id = profile!.Id;

        // Establish friendship
        // User 2 sends request using username
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2Token);
        await Client.PostAsync($"/api/friends/{profile!.UserName}", null);
        
        // User 1 accepts
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);
        var requestsResponse = await Client.GetAsync("/api/friends/requests/incoming");
        var requests = await requestsResponse.Content.ReadFromJsonAsync<List<FriendRequestDto>>();
        var requestId = requests!.First().Id;
        await Client.PostAsJsonAsync($"/api/friends/requests/{requestId}/respond", new RespondFriendRequest { Accept = true });

        // Act - User 2 gets User 1 metrics
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2Token);
        var response = await Client.GetAsync($"/api/body-metrics/friends/{user1Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var metrics = await response.Content.ReadFromJsonAsync<List<BodyMeasurementDto>>();
        metrics.Should().HaveCount(1);
        metrics!.First().WeightKg.Should().Be(80);
    }

    [Fact]
    public async Task GetFriendMetrics_ShouldReturnEmpty_WhenFriendDoesNotShare()
    {
        // Arrange
        var user1Token = await RegisterAndGetTokenAsync();
        var user2Token = await RegisterAndGetTokenAsync();
        
        // User 1 keeps sharing disabled (default)
        
        // User 1 adds measurement
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);
        var dto = new CreateBodyMeasurementDto(80, 180, null, null, null, null, null, null, null);
        await Client.PostAsJsonAsync("/api/body-metrics", dto);

        // Get User 1 ID
        var profileResponse = await Client.GetAsync("/api/userprofile");
        var profile = await profileResponse.Content.ReadFromJsonAsync<UserProfileDto>();
        var user1Id = profile!.Id;

        // Establish friendship
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2Token);
        await Client.PostAsync($"/api/friends/{profile!.UserName}", null);
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);
        var requestsResponse = await Client.GetAsync("/api/friends/requests/incoming");
        var requests = await requestsResponse.Content.ReadFromJsonAsync<List<FriendRequestDto>>();
        var requestId = requests!.First().Id;
        await Client.PostAsJsonAsync($"/api/friends/requests/{requestId}/respond", new RespondFriendRequest { Accept = true });

        // Act - User 2 gets User 1 metrics
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user2Token);
        var response = await Client.GetAsync($"/api/body-metrics/friends/{user1Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var metrics = await response.Content.ReadFromJsonAsync<List<BodyMeasurementDto>>();
        metrics.Should().BeEmpty();
    }

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
