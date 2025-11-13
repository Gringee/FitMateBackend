using Application.Abstractions;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/friends")]
public class FriendsController(IFriendshipService svc) : ControllerBase
{
    // POST /api/friends/requests/{username}
    [HttpPost("{username}")]
    public async Task<IActionResult> SendRequest(string username, CancellationToken ct)
    {
        await svc.SendRequestByUserNameAsync(username, ct);
        return Ok(new { message = "Wyslano zaproszenie." });
    }

    // POST /api/friends/requests/{requestId}/respond
    [HttpPost("requests/{requestId:guid}/respond")]
    public async Task<IActionResult> Respond(Guid requestId, [FromBody] RespondFriendRequest body, CancellationToken ct)
    {
        await svc.RespondAsync(requestId, body.Accept, ct);
        return Ok(new { message = body.Accept ? "Zaakceptowano." : "Odrzucono." });
    }

    // GET /api/friends
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FriendDto>>> GetFriends(CancellationToken ct)
        => Ok(await svc.GetFriendsAsync(ct));

    // GET /api/friends/requests/incoming
    [HttpGet("requests/incoming")]
    public async Task<ActionResult<IReadOnlyList<FriendRequestDto>>> Incoming(CancellationToken ct)
        => Ok(await svc.GetIncomingAsync(ct));

    // GET /api/friends/requests/outgoing
    [HttpGet("requests/outgoing")]
    public async Task<ActionResult<IReadOnlyList<FriendRequestDto>>> Outgoing(CancellationToken ct)
        => Ok(await svc.GetOutgoingAsync(ct));

    // DELETE /api/friends/{friendUserId}
    [HttpDelete("{friendUserId:guid}")]
    public async Task<IActionResult> Remove(Guid friendUserId, CancellationToken ct)
        => await svc.RemoveFriendAsync(friendUserId, ct) ? NoContent() : NotFound();
}