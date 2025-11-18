using Application.Abstractions;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace WebApi.Controllers;

/// <summary>
/// Zarządzanie relacjami znajomości (Friendships).
/// </summary>
/// <remarks>
/// Kontroler obsługuje pełny cykl życia znajomości:
/// wysyłanie zaproszeń, akceptowanie/odrzucanie, listowanie znajomych oraz ich usuwanie.
/// </remarks>
[Authorize]
[ApiController]
[Route("api/friends")]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class FriendsController : ControllerBase
{
    private readonly IFriendshipService _svc;

    public FriendsController(IFriendshipService svc)
        => _svc = svc;

    /// <summary>
    /// Wysyła zaproszenie do znajomych (po nazwie użytkownika).
    /// </summary>
    /// <remarks>
    /// **Logika biznesowa:**
    /// * Nie można wysłać zaproszenia do samego siebie.
    /// * Jeśli użytkownicy są już znajomymi, zwrócony zostanie błąd.
    /// * **Auto-akceptacja:** Jeśli użytkownik, do którego wysyłasz zaproszenie, wcześniej wysłał zaproszenie do Ciebie (status Pending),
    /// system wykryje to i automatycznie nawiąże relację (status Accepted).
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     POST /api/friends/jankowalski
    ///     
    /// </remarks>
    /// <param name="username">Unikalna nazwa użytkownika (UserName) adresata.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="200">
    /// Zaproszenie wysłane pomyślnie LUB relacja została automatycznie zaakceptowana (gdy istniało zaproszenie zwrotne).
    /// </response>
    /// <response code="400">
    /// Błąd walidacji logicznej. Przykładowe komunikaty błędów:
    /// * "You cannot add yourself."
    /// * "You are already friends."
    /// * "A friend request is already in progress." (gdy już wysłałeś wcześniej zaproszenie).
    /// </response>
    /// <response code="404">Nie znaleziono użytkownika o podanej nazwie.</response>
    [HttpPost("{username}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendRequest(
        string username,
        CancellationToken ct)
    {
        // Wyjątki (KeyNotFound, InvalidOperation) są łapane przez Middleware -> 404 / 400
        await _svc.SendRequestByUserNameAsync(username, ct);
        return Ok(new { message = "Friend request sent." });
    }

    /// <summary>
    /// Odpowiada na otrzymane zaproszenie (Akceptacja / Odrzucenie).
    /// </summary>
    /// <remarks>
    /// Pozwala zaakceptować lub odrzucić oczekujące zaproszenie.
    /// Można odpowiadać tylko na zaproszenia, w których jest się adresatem (`ToUserId`).
    /// 
    /// **Przykładowe żądanie (Akceptacja):**
    /// 
    ///     POST /api/friends/requests/3fa85f64-5717-4562-b3fc-2c963f66afa6/respond
    ///     {
    ///        "accept": true
    ///     }
    ///     
    /// </remarks>
    /// <param name="requestId">Identyfikator zaproszenia (GUID).</param>
    /// <param name="body">Obiekt decyzyjny (Accept = true/false).</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="200">Decyzja została zapisana (znajomość nawiązana lub zaproszenie odrzucone).</response>
    /// <response code="400">
    /// Zaproszenie nie jest w stanie "Pending" lub próbujesz odpowiedzieć na własne zaproszenie.
    /// </response>
    /// <response code="404">Zaproszenie nie istnieje.</response>
    /// <response code="401">Próba odpowiedzi na zaproszenie, które nie jest skierowane do Ciebie.</response>
    [HttpPost("requests/{requestId:guid}/respond")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Respond(
        Guid requestId,
        [FromBody] RespondFriendRequest body,
        CancellationToken ct)
    {
        await _svc.RespondAsync(requestId, body.Accept, ct);
        return Ok(new { message = body.Accept ? "Friend request accepted." : "Friend request rejected." });
    }

    /// <summary>
    /// Pobiera listę aktualnych znajomych.
    /// </summary>
    /// <remarks>
    /// Zwraca listę użytkowników, z którymi relacja ma status <c>Accepted</c>.
    /// </remarks>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Lista obiektów zawierających ID i nazwę użytkownika znajomych.</returns>
    /// <response code="200">Lista znajomych.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FriendDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FriendDto>>> GetFriends(
        CancellationToken ct)
    {
        var friends = await _svc.GetFriendsAsync(ct);
        return Ok(friends);
    }

    /// <summary>
    /// Pobiera listę oczekujących zaproszeń przychodzących.
    /// </summary>
    /// <remarks>
    /// Są to zaproszenia wysłane przez inne osoby do aktualnie zalogowanego użytkownika.
    /// </remarks>
    /// <response code="200">Lista zaproszeń do rozpatrzenia.</response>
    [HttpGet("requests/incoming")]
    [ProducesResponseType(typeof(IReadOnlyList<FriendRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FriendRequestDto>>> Incoming(
        CancellationToken ct)
    {
        var items = await _svc.GetIncomingAsync(ct);
        return Ok(items);
    }

    /// <summary>
    /// Pobiera listę wysłanych zaproszeń (wychodzących).
    /// </summary>
    /// <remarks>
    /// Są to zaproszenia wysłane przez Ciebie, które wciąż oczekują na decyzję drugiej strony (status <c>Pending</c>).
    /// </remarks>
    /// <response code="200">Lista wysłanych, oczekujących zaproszeń.</response>
    [HttpGet("requests/outgoing")]
    [ProducesResponseType(typeof(IReadOnlyList<FriendRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FriendRequestDto>>> Outgoing(
        CancellationToken ct)
    {
        var items = await _svc.GetOutgoingAsync(ct);
        return Ok(items);
    }

    /// <summary>
    /// Usuwa użytkownika z listy znajomych.
    /// </summary>
    /// <remarks>
    /// Trwale usuwa relację znajomości (status <c>Accepted</c>). Operacja jest dwustronna – użytkownik znika również z listy znajomych drugiej osoby.
    /// </remarks>
    /// <param name="friendUserId">Identyfikator użytkownika (GUID), którego chcemy usunąć.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="204">Znajomy został usunięty pomyślnie.</response>
    /// <response code="404">Taka relacja nie istnieje (nie jesteście znajomymi).</response>
    [HttpDelete("{friendUserId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(
        Guid friendUserId,
        CancellationToken ct)
    {
        var removed = await _svc.RemoveFriendAsync(friendUserId, ct);
        return removed ? NoContent() : NotFound();
    }
}