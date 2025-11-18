using Application.Abstractions;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace WebApi.Controllers;

/// <summary>
/// Zarządzanie aktywnymi sesjami treningowymi.
/// </summary>
/// <remarks>
/// Kontroler obsługuje pełny cykl życia sesji treningowej:
/// od jej rozpoczęcia na podstawie planu, poprzez logowanie wyników poszczególnych serii w czasie rzeczywistym,
/// aż po jej zakończenie lub anulowanie.
/// </remarks>
[Authorize]
[ApiController]
[Route("api/sessions")]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class SessionsController : ControllerBase
{
    private readonly IWorkoutSessionService _svc;

    public SessionsController(IWorkoutSessionService svc)
        => _svc = svc;

    /// <summary>
    /// Rozpoczyna nową sesję treningową (Live Workout).
    /// </summary>
    /// <remarks>
    /// Tworzy nową sesję na podstawie zaplanowanego treningu (<c>ScheduledWorkout</c>).
    /// Kopiuje wszystkie ćwiczenia i serie z planu do sesji, ustawiając jej status na <c>in_progress</c>.
    /// 
    /// **Działanie:**
    /// * Wymaga podania ID zaplanowanego treningu.
    /// * Jeśli zaplanowany trening nie istnieje lub nie należy do użytkownika, zwraca 404.
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     POST /api/sessions/start
    ///     {
    ///        "scheduledId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    ///     }
    ///     
    /// </remarks>
    /// <param name="request">Obiekt zawierający ID zaplanowanego treningu.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Nowo utworzona sesja treningowa.</returns>
    /// <response code="201">Sesja została wystartowana.</response>
    /// <response code="400">Błąd walidacji (np. pusty GUID).</response>
    /// <response code="404">Nie znaleziono zaplanowanego treningu.</response>
    [HttpPost("start")]
    [ProducesResponseType(typeof(WorkoutSessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkoutSessionDto>> Start(
        [FromBody] StartSessionRequest request,
        CancellationToken ct)
    {
        var dto = await _svc.StartAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Częściowo aktualizuje wyniki pojedynczej serii w trakcie sesji.
    /// </summary>
    /// <remarks>
    /// Aktualizuje dane o wykonanej serii (powtórzenia, ciężar, RPE) w trwającej sesji.
    /// 
    /// **Logika biznesowa:**
    /// * Sesja musi być w statusie <c>in_progress</c>.
    /// * Identyfikacja serii odbywa się poprzez `ExerciseOrder` (kolejność ćwiczenia) oraz `SetNumber` (numer serii).
    /// * Nie musisz przesyłać wszystkich pól – metoda służy do aktualizacji "na bieżąco".
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     PATCH /api/sessions/{id}/set
    ///     {
    ///        "exerciseOrder": 1,
    ///        "setNumber": 2,
    ///        "repsDone": 10,
    ///        "weightDone": 80.5,
    ///        "rpe": 8,
    ///        "isFailure": false
    ///     }
    ///     
    /// </remarks>
    /// <param name="id">Identyfikator trwającej sesji.</param>
    /// <param name="req">Dane aktualizujące serię.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Zaktualizowany obiekt całej sesji.</returns>
    /// <response code="200">Seria została zaktualizowana.</response>
    /// <response code="400">
    /// Błąd walidacji (np. ujemne wartości) LUB sesja nie jest w toku (np. jest już zakończona).
    /// </response>
    /// <response code="404">Sesja, ćwiczenie lub seria nie została znaleziona.</response>
    [HttpPatch("{sessionId:guid}/sets/{setId:guid}")]
    [ProducesResponseType(typeof(WorkoutSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkoutSessionDto>> PatchSet(
        Guid sessionId,
        Guid setId,
        [FromBody] PatchSetRequest req,
        CancellationToken ct)
    {
        var dto = await _svc.PatchSetAsync(sessionId, setId, req, ct);
        return Ok(dto);
    }

    /// <summary>
    /// Dodaje dodatkowe ćwiczenie do trwającej sesji (Ad-Hoc).
    /// </summary>
    /// <remarks>
    /// Pozwala dodać niezaplanowane wcześniej ćwiczenie do aktywnej sesji.
    /// 
    /// **Zasady:**
    /// * Ćwiczenie jest dodawane tylko do tej konkretnej sesji (nie zmienia planu treningowego).
    /// * Jeśli nie podasz `Order`, ćwiczenie zostanie dodane na koniec listy.
    /// * Wymagane jest zdefiniowanie przynajmniej jednej serii.
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     POST /api/sessions/{id}/exercises
    ///     {
    ///        "name": "Dodatkowe Pompki",
    ///        "restSecPlanned": 60,
    ///        "sets": [
    ///           { "repsPlanned": 15, "weightPlanned": 0 }
    ///        ]
    ///     }
    /// </remarks>
    /// <param name="id">Identyfikator sesji.</param>
    /// <param name="req">Definicja nowego ćwiczenia.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Zaktualizowana sesja z nowym ćwiczeniem.</returns>
    /// <response code="200">Ćwiczenie dodane pomyślnie.</response>
    /// <response code="400">Błąd walidacji lub sesja nie jest w toku.</response>
    /// <response code="404">Sesja nie istnieje.</response>
    [HttpPost("{id:guid}/exercises")]
    [ProducesResponseType(typeof(WorkoutSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkoutSessionDto>> AddExercise(
        Guid id,
        [FromBody] AddSessionExerciseRequest req,
        CancellationToken ct)
    {
        var dto = await _svc.AddExerciseAsync(id, req, ct);
        return Ok(dto);
    }

    /// <summary>
    /// Kończy sesję treningową (Success).
    /// </summary>
    /// <remarks>
    /// Zmienia status sesji na <c>completed</c>, wylicza czas trwania i oznacza powiązany plan w kalendarzu jako wykonany.
    /// Po wykonaniu tej operacji nie można już modyfikować serii ani dodawać ćwiczeń.
    /// 
    /// **Opcjonalne parametry:**
    /// * `CompletedAtUtc`: Czas zakończenia (domyślnie <c>DateTime.UtcNow</c>).
    /// * `SessionNotes`: Notatka końcowa do treningu.
    /// </remarks>
    /// <param name="id">Identyfikator sesji.</param>
    /// <param name="req">Opcjonalne dane kończące sesję.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Zakończona sesja.</returns>
    /// <response code="200">Sesja zakończona pomyślnie.</response>
    /// <response code="400">Próba zakończenia sesji, która nie jest w toku.</response>
    /// <response code="404">Sesja nie istnieje.</response>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(WorkoutSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkoutSessionDto>> Complete(
        Guid id,
        [FromBody] CompleteSessionRequest req,
        CancellationToken ct)
    {
        var dto = await _svc.CompleteAsync(id, req, ct);
        return Ok(dto);
    }

    /// <summary>
    /// Przerywa/Anuluje sesję treningową.
    /// </summary>
    /// <remarks>
    /// Zmienia status sesji na <c>aborted</c>. Używane, gdy użytkownik musi nagle przerwać trening (np. kontuzja, brak czasu).
    /// </remarks>
    /// <param name="id">Identyfikator sesji.</param>
    /// <param name="req">Powód przerwania sesji.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Przerwana sesja.</returns>
    /// <response code="200">Sesja została anulowana.</response>
    /// <response code="400">Próba anulowania sesji, która nie jest w toku.</response>
    /// <response code="404">Sesja nie istnieje.</response>
    [HttpPost("{id:guid}/abort")]
    [ProducesResponseType(typeof(WorkoutSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkoutSessionDto>> Abort(
        Guid id,
        [FromBody] AbortSessionRequest req,
        CancellationToken ct)
    {
        var dto = await _svc.AbortAsync(id, req, ct);
        return Ok(dto);
    }

    /// <summary>
    /// Pobiera szczegóły sesji treningowej.
    /// </summary>
    /// <remarks>
    /// Zwraca pełny stan sesji: status, czasy, listę ćwiczeń oraz stan wszystkich serii.
    /// </remarks>
    /// <param name="id">Identyfikator sesji.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Obiekt DTO sesji.</returns>
    /// <response code="200">Szczegóły sesji.</response>
    /// <response code="404">Sesja nie istnieje.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkoutSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkoutSessionDto>> GetById(
        Guid id,
        CancellationToken ct)
    {
        var dto = await _svc.GetByIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>
    /// Pobiera historię sesji w zadanym przedziale czasu.
    /// </summary>
    /// <remarks>
    /// Służy do wyświetlania historii treningów lub statystyk.
    /// Daty powinny być przekazane w formacie ISO 8601 (UTC).
    /// 
    /// **Przykładowe zapytanie:**
    /// 
    ///     GET /api/sessions/by-range?fromUtc=2023-11-01T00:00:00Z&amp;toUtc=2023-11-30T23:59:59Z
    ///     
    /// </remarks>
    /// <param name="query">Parametry filtrujące (FromUtc, ToUtc).</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Lista sesji w zadanym okresie.</returns>
    /// <response code="200">Lista sesji.</response>
    /// <response code="400">Nieprawidłowy zakres dat (np. To &lt; From).</response>
    [HttpGet("by-range")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkoutSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<WorkoutSessionDto>>> ByRange(
        [FromQuery] SessionsByRangeRequest query,
        CancellationToken ct)
    {
        // Zakładam, że SessionsByRangeRequest ma metodę walidacji lub logikę konwersji,
        // tutaj wywołujemy metodę pomocniczą z twojego kodu:
        var (fromUtc, toUtc) = query.NormalizeToUtc();

        var list = await _svc.GetByRangeAsync(fromUtc, toUtc, ct);
        return Ok(list);
    }
}