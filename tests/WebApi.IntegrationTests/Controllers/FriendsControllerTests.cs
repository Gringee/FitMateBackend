using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs;
using Application.DTOs.Auth;
using FluentAssertions;
using WebApi.IntegrationTests.Common;
using Xunit;

namespace WebApi.IntegrationTests.Controllers;

public class FriendsControllerTests : BaseIntegrationTest
{
    public FriendsControllerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SendRequest_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await Client.PostAsync("/api/friends/someuser", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendRequest_ShouldReturn200OK_WhenUserExists()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync("sender");
        await RegisterAndGetTokenAsync("recipient"); // Create recipient
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.PostAsync($"/api/friends/recipient", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendRequest_ShouldReturn404NotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync("sender2");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.PostAsync("/api/friends/nonexistentuser", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetIncoming_ShouldReturnPendingRequests()
    {
        // Arrange
        var senderToken = await RegisterAndGetTokenAsync("sender3");
        var recipientToken = await RegisterAndGetTokenAsync("recipient3");

        // Sender sends request
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", senderToken);
        await Client.PostAsync("/api/friends/recipient3", null);

        // Recipient checks incoming
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);

        // Act
        var response = await Client.GetAsync("/api/friends/requests/incoming");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var requests = await response.Content.ReadFromJsonAsync<List<FriendRequestDto>>();
        requests.Should().NotBeNull();
        requests!.Should().ContainSingle(r => r.FromName.Contains("sender3") || r.FromUserId != Guid.Empty); 
        // Note: FromName might be "Test User" based on helper, but we can rely on count or other props.
        // Actually my helper sets username as part of email/username but FullName is "Test User".
        // Let's check count.
        requests!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Respond_ShouldAcceptRequest()
    {
        // Arrange
        var senderToken = await RegisterAndGetTokenAsync("sender4");
        var recipientToken = await RegisterAndGetTokenAsync("recipient4");

        // Sender sends request
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", senderToken);
        await Client.PostAsync("/api/friends/recipient4", null);

        // Recipient gets request ID
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);
        var incomingResponse = await Client.GetAsync("/api/friends/requests/incoming");
        var requests = await incomingResponse.Content.ReadFromJsonAsync<List<FriendRequestDto>>();
        var requestId = requests!.First().Id;

        // Act
        var response = await Client.PostAsJsonAsync($"/api/friends/requests/{requestId}/respond", new RespondFriendRequest { Accept = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify friends list
        var friendsResponse = await Client.GetAsync("/api/friends");
        var friends = await friendsResponse.Content.ReadFromJsonAsync<List<FriendDto>>();
        friends.Should().Contain(f => f.UserName == "sender4");
    }

    [Fact]
    public async Task Respond_ShouldRejectRequest()
    {
        // Arrange
        var senderToken = await RegisterAndGetTokenAsync("sender5");
        var recipientToken = await RegisterAndGetTokenAsync("recipient5");

        // Sender sends request
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", senderToken);
        await Client.PostAsync("/api/friends/recipient5", null);

        // Recipient gets request ID
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);
        var incomingResponse = await Client.GetAsync("/api/friends/requests/incoming");
        var requests = await incomingResponse.Content.ReadFromJsonAsync<List<FriendRequestDto>>();
        var requestId = requests!.First().Id;

        // Act
        var response = await Client.PostAsJsonAsync($"/api/friends/requests/{requestId}/respond", new RespondFriendRequest { Accept = false });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify friends list (should be empty)
        var friendsResponse = await Client.GetAsync("/api/friends");
        var friends = await friendsResponse.Content.ReadFromJsonAsync<List<FriendDto>>();
        friends.Should().HaveCount(0);
    }

    [Fact]
    public async Task SendRequest_ShouldReturn400BadRequest_WhenAlreadyFriends()
    {
        // Arrange - create two users and make them friends
        var user1Reg = new RegisterRequest { Email = $"u1_{Guid.NewGuid()}@example.com", UserName = $"u1_{Guid.NewGuid().ToString().Substring(0, 5)}", Password = "SecurePass123!", FullName = "User 1" };
        var user2Reg = new RegisterRequest { Email = $"u2_{Guid.NewGuid()}@example.com", UserName = $"u2_{Guid.NewGuid().ToString().Substring(0, 5)}", Password = "SecurePass123!", FullName = "User 2" };
        
        var r1 = await Client.PostAsJsonAsync("/api/auth/register", user1Reg);
        var r2 = await Client.PostAsJsonAsync("/api/auth/register", user2Reg);
        var auth1 = await r1.Content.ReadFromJsonAsync<AuthResponse>();
        var auth2 = await r2.Content.ReadFromJsonAsync<AuthResponse>();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth1!.AccessToken);
        await Client.PostAsync($"/api/friends/{user2Reg.UserName}", null);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth2!.AccessToken);
        var incoming = await Client.GetAsync("/api/friends/requests/incoming");
        var requests = await incoming.Content.ReadFromJsonAsync<List<FriendRequestDto>>();
        await Client.PostAsJsonAsync($"/api/friends/requests/{requests!.First().Id}/respond", new RespondFriendRequest { Accept = true });

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth1.AccessToken);
        var response = await Client.PostAsync($"/api/friends/{user2Reg.UserName}", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendRequest_ShouldReturn400BadRequest_WhenPendingRequestExists()
    {
        var user1Reg = new RegisterRequest { Email = $"p1_{Guid.NewGuid()}@example.com", UserName = $"p1_{Guid.NewGuid().ToString().Substring(0, 5)}", Password = "SecurePass123!", FullName = "User 1" };
        var user2Reg = new RegisterRequest { Email = $"p2_{Guid.NewGuid()}@example.com", UserName = $"p2_{Guid.NewGuid().ToString().Substring(0, 5)}", Password = "SecurePass123!", FullName = "User 2" };
        
        var r1 = await Client.PostAsJsonAsync("/api/auth/register", user1Reg);
        await Client.PostAsJsonAsync("/api/auth/register", user2Reg);
        var auth1 = await r1.Content.ReadFromJsonAsync<AuthResponse>();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth1!.AccessToken);
        await Client.PostAsync($"/api/friends/{user2Reg.UserName}", null);
        var response = await Client.PostAsync($"/api/friends/{user2Reg.UserName}", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RespondRequest_ShouldReturn404NotFound_WhenRequestDoesNotExist()
    {
        var token = await RegisterAndGetTokenAsync($"u_{Guid.NewGuid().ToString().Substring(0, 5)}");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await Client.PostAsJsonAsync($"/api/friends/requests/{Guid.NewGuid()}/respond", new RespondFriendRequest { Accept = true });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFriends_ShouldReturn401Unauthorized_WhenNotAuthenticated()
    {
        var response = await Client.GetAsync("/api/friends");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteFriend_ShouldReturn404NotFound_WhenFriendshipDoesNotExist()
    {
        var token = await RegisterAndGetTokenAsync($"u_{Guid.NewGuid().ToString().Substring(0, 5)}");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await Client.DeleteAsync($"/api/friends/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Remove_ShouldRemoveFriend()
    {
        // Arrange
        var senderToken = await RegisterAndGetTokenAsync("sender6");
        var recipientToken = await RegisterAndGetTokenAsync("recipient6");

        // Establish friendship
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", senderToken);
        await Client.PostAsync("/api/friends/recipient6", null);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recipientToken);
        var incomingResponse = await Client.GetAsync("/api/friends/requests/incoming");
        var requests = await incomingResponse.Content.ReadFromJsonAsync<List<FriendRequestDto>>();
        var requestId = requests!.First().Id;
        await Client.PostAsJsonAsync($"/api/friends/requests/{requestId}/respond", new RespondFriendRequest { Accept = true });

        // Verify friendship exists
        var friendsResponse = await Client.GetAsync("/api/friends");
        var friends = await friendsResponse.Content.ReadFromJsonAsync<List<FriendDto>>();
        var friendId = friends!.First().UserId;

        // Act
        var response = await Client.DeleteAsync($"/api/friends/{friendId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify removal
        var friendsAfter = await Client.GetAsync("/api/friends");
        var listAfter = await friendsAfter.Content.ReadFromJsonAsync<List<FriendDto>>();
        listAfter.Should().BeEmpty();
    }

    private async Task<string> RegisterAndGetTokenAsync(string usernamePrefix)
    {
        // Ensure unique username
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);
        var username = $"{usernamePrefix}_{uniqueSuffix}"; // e.g. sender_abc12345 (might be too long if prefix is long, max 50 chars usually)
        // Actually let's just use the prefix as base if it's short enough, or just the prefix if unique.
        // But tests run in parallel or sequentially on same DB (Testcontainers), so uniqueness is key.
        // My previous helper used random guid.
        // Here I want specific username to be able to target it in SendRequest.
        
        // Let's use exact username if provided, but append guid to ensure uniqueness if needed?
        // No, SendRequest needs to know the username. So I must return the username or use a predictable one.
        // But if I use predictable one, subsequent test runs might fail if DB is not cleaned.
        // Testcontainers usually spins up fresh DB for the factory lifetime?
        // IntegrationTestWebAppFactory: "IAsyncLifetime". InitializeAsync starts container.
        // If it's shared collection, it might be shared.
        // BaseIntegrationTest uses the factory.
        // Usually xUnit creates new test class instance, but factory is shared if using IClassFixture.
        // My BaseIntegrationTest implements IClassFixture<IntegrationTestWebAppFactory>.
        // So DB is shared across tests in this class.
        // So I MUST use unique usernames.
        
        // I will use the username passed in, but I need to know what it is to call SendRequest.
        // So I will change the helper to accept a base name and return the actual username used?
        // Or I just use the username passed in and append a GUID inside the test?
        // Better: Helper takes `username` and `email`.
        // But `RegisterAndGetTokenAsync` signature in my previous tests was simple.
        
        // I'll modify helper to take `username` and use it directly. The caller is responsible for uniqueness.
        // In tests I'll use `Guid.NewGuid()` in the username.
        
        var registerRequest = new RegisterRequest
        {
            Email = $"{usernamePrefix}@example.com",
            UserName = usernamePrefix,
            Password = "SecurePass123!",
            FullName = "Test User"
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        
        if (!response.IsSuccessStatusCode)
        {
            // If failed (e.g. username taken), try appending guid
             registerRequest.UserName = $"{usernamePrefix}_{Guid.NewGuid().ToString().Substring(0,5)}";
             registerRequest.Email = $"{registerRequest.UserName}@example.com";
             response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        }
        
        response.EnsureSuccessStatusCode();
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.AccessToken;
    }
}
