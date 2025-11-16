using Application.Abstractions;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Zarządzanie zaproszeniami do znajomych oraz listą znajomych zalogowanego użytkownika.
/// </summary>
[Authorize]
[ApiController]
[Route("api/friends")]
[Produces("application/json")]
public class FriendsController : ControllerBase
{
    private readonly IFriendshipService _svc;

    public FriendsController(IFriendshipService svc)
        => _svc = svc;

    /// <summary>
    /// Wysyła zaproszenie do znajomych na podstawie nazwy użytkownika (UserName).
    /// </summary>
    /// <remarks>
    /// Jeśli zaproszenie już istnieje w statusie <c>Pending</c> i zostało wysłane w drugą stronę,
    /// serwis może automatycznie je zaakceptować.<br/>
    /// Rzucane wyjątki (np. gdy użytkownik nie istnieje, próba dodania siebie, itp.) są mapowane
    /// przez globalny middleware na odpowiednie kody HTTP.
    /// </remarks>
    /// <param name="username">Nazwa użytkownika (UserName), do którego wysyłamy zaproszenie.</param>
    /// <response code="200">Zaproszenie zostało wysłane lub automatycznie zaakceptowane.</response>
    /// <response code="400">Błąd biznesowy (np. zaproszenie już istnieje, próba dodania siebie).</response>
    /// <response code="401">Brak autoryzacji.</response>
    /// <response code="404">Użytkownik o podanym username nie istnieje.</response>
    [HttpPost("{username}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendRequest(
        string username,
        CancellationToken ct)
    {
        await _svc.SendRequestByUserNameAsync(username, ct);
        return Ok(new { message = "Friend request sent." });
    }

    /// <summary>
    /// Akceptuje lub odrzuca zaproszenie do znajomych.
    /// </summary>
    /// <remarks>
    /// Użytkownik może odpowiadać tylko na zaproszenia, których jest adresatem (nie nadawcą).
    /// </remarks>
    /// <param name="requestId">Identyfikator zaproszenia.</param>
    /// <param name="body">Informacja, czy zaakceptować (<c>true</c>) czy odrzucić (<c>false</c>).</param>
    /// <response code="200">Zaproszenie zaakceptowane lub odrzucone.</response>
    /// <response code="400">Zaproszenie zostało już wcześniej rozpatrzone lub inne naruszenie reguł.</response>
    /// <response code="401">Brak autoryzacji.</response>
    /// <response code="404">Zaproszenie nie istnieje lub nie dotyczy zalogowanego użytkownika.</response>
    [HttpPost("requests/{requestId:guid}/respond")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Respond(
        Guid requestId,
        [FromBody] RespondFriendRequest body,
        CancellationToken ct)
    {
        await _svc.RespondAsync(requestId, body.Accept, ct);
        return Ok(new { message = body.Accept ? "Friend request accepted." : "Friend request rejected." });
    }

    /// <summary>
    /// Zwraca listę zaakceptowanych znajomych zalogowanego użytkownika.
    /// </summary>
    /// <response code="200">Lista znajomych.</response>
    /// <response code="401">Brak autoryzacji.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FriendDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<FriendDto>>> GetFriends(
        CancellationToken ct)
    {
        var friends = await _svc.GetFriendsAsync(ct);
        return Ok(friends);
    }

    /// <summary>
    /// Zwraca listę przychodzących (otrzymanych) zaproszeń do znajomych.
    /// </summary>
    /// <response code="200">Lista oczekujących zaproszeń przychodzących.</response>
    /// <response code="401">Brak autoryzacji.</response>
    [HttpGet("requests/incoming")]
    [ProducesResponseType(typeof(IReadOnlyList<FriendRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<FriendRequestDto>>> Incoming(
        CancellationToken ct)
    {
        var items = await _svc.GetIncomingAsync(ct);
        return Ok(items);
    }

    /// <summary>
    /// Zwraca listę wychodzących (wysłanych) zaproszeń do znajomych.
    /// </summary>
    /// <response code="200">Lista oczekujących zaproszeń wysłanych.</response>
    /// <response code="401">Brak autoryzacji.</response>
    [HttpGet("requests/outgoing")]
    [ProducesResponseType(typeof(IReadOnlyList<FriendRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<FriendRequestDto>>> Outgoing(
        CancellationToken ct)
    {
        var items = await _svc.GetOutgoingAsync(ct);
        return Ok(items);
    }

    /// <summary>
    /// Usuwa istniejącego znajomego (kończy relację znajomości).
    /// </summary>
    /// <remarks>
    /// Usuwa tylko relację, w której zalogowany użytkownik jest jedną ze stron.
    /// </remarks>
    /// <param name="friendUserId">Id użytkownika, którego chcemy usunąć z listy znajomych.</param>
    /// <response code="204">Znajomy został usunięty.</response>
    /// <response code="404">Relacja znajomości nie istnieje.</response>
    /// <response code="401">Brak autoryzacji.</response>
    [HttpDelete("{friendUserId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(
        Guid friendUserId,
        CancellationToken ct)
    {
        var removed = await _svc.RemoveFriendAsync(friendUserId, ct);
        return removed ? NoContent() : NotFound();
    }
}