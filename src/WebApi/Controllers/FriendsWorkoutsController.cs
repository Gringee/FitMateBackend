using Application.Abstractions;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace WebApi.Controllers;

/// <summary>
/// Przeglądanie aktywności treningowej znajomych.
/// </summary>
/// <remarks>
/// Kontroler umożliwia podgląd zaplanowanych treningów oraz historii odbytych sesji
/// osób, które znajdują się na liście zaakceptowanych znajomych.
/// 
/// **Prywatność:**
/// Dane są zwracane tylko wtedy, gdy znajomy wyraził na to zgodę, ustawiając
/// flagę <c>VisibleToFriends</c> w swoim treningu.
/// </remarks>
[Authorize]
[ApiController]
[Route("api/friends/workouts")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class FriendsWorkoutsController : ControllerBase
{
    private readonly IFriendWorkoutService _svc;

    public FriendsWorkoutsController(IFriendWorkoutService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// Pobiera kalendarz treningowy znajomych w zadanym przedziale dat.
    /// </summary>
    /// <remarks>
    /// Zwraca listę zaplanowanych treningów (Scheduled Workouts) należących do Twoich znajomych.
    /// 
    /// **Zasady filtrowania:**
    /// * Uwzględniani są tylko znajomi ze statusem relacji <c>Accepted</c>.
    /// * Trening musi mieć ustawioną flagę <c>IsVisibleToFriends = true</c>.
    /// * Przedział dat jest domknięty (włącznie z <c>from</c> i <c>to</c>).
    /// 
    /// **Wymagany format daty:** <c>yyyy-MM-dd</c>
    /// 
    /// **Przykładowe zapytanie:**
    /// 
    ///     GET /api/friends/workouts/scheduled?from=2025-11-01&amp;to=2025-11-30
    /// 
    /// </remarks>
    /// <param name="query">Parametry zakresu dat (Od - Do).</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Lista udostępnionych, zaplanowanych treningów znajomych.</returns>
    /// <response code="200">Pobrano listę treningów.</response>
    /// <response code="400">
    /// Niepoprawny format daty lub błąd logiczny (np. data "Do" jest wcześniejsza niż "Od").
    /// </response>
    /// <response code="401">Użytkownik niezalogowany.</response>
    [HttpGet("scheduled")]
    [ProducesResponseType(typeof(IReadOnlyList<FriendScheduledWorkoutDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<FriendScheduledWorkoutDto>>> GetFriendsScheduled(
        [FromQuery] FriendsScheduledRangeRequest query,
        CancellationToken ct)
    {
        // Walidacja zakresu dat (From <= To) oraz formatu odbywa się automatycznie
        // dzięki interfejsowi IValidatableObject w DTO.
        var (fromDate, toDate) = query.Normalize();
        var list = await _svc.GetFriendsScheduledAsync(fromDate, toDate, ct);
        return Ok(list);
    }

    /// <summary>
    /// Pobiera historię sesji treningowych znajomych w zadanym przedziale czasu.
    /// </summary>
    /// <remarks>
    /// Zwraca listę sesji (rozpoczętych/zakończonych treningów) Twoich znajomych.
    /// 
    /// **Zasady filtrowania:**
    /// * Zakres dotyczy daty rozpoczęcia sesji (<c>StartedAtUtc</c>).
    /// * Zwracane są sesje powiązane z zaplanowanymi treningami, które są publiczne dla znajomych.
    /// * Przedział czasu jest lewostronnie domknięty: <c>start &gt;= fromUtc</c> oraz <c>start &lt; toUtc</c>.
    /// 
    /// **Wymagany format:** ISO 8601 UTC (np. <c>2025-11-01T15:30:00Z</c>).
    /// 
    /// **Przykładowe zapytanie:**
    /// 
    ///     GET /api/friends/workouts/sessions?fromUtc=2025-11-01T00:00:00Z&amp;toUtc=2025-11-02T00:00:00Z
    /// 
    /// </remarks>
    /// <param name="query">Parametry zakresu czasu (UTC).</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Lista udostępnionych sesji treningowych znajomych.</returns>
    /// <response code="200">Pobrano listę sesji.</response>
    /// <response code="400">
    /// Niepoprawny format ISO 8601 lub data końcowa jest wcześniejsza niż początkowa.
    /// </response>
    /// <response code="401">Użytkownik niezalogowany.</response>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<FriendWorkoutSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<FriendWorkoutSessionDto>>> GetFriendsSessions(
        [FromQuery] FriendsSessionsRangeRequest query,
        CancellationToken ct)
    {
        var (fromUtc, toUtc) = query.NormalizeToUtc();
        var list = await _svc.GetFriendsSessionsAsync(fromUtc, toUtc, ct);
        return Ok(list);
    }
}