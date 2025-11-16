using Application.Abstractions;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Operacje na sesjach treningowych (realizacja zaplanowanych treningów).
/// </summary>
[Authorize]
[ApiController]
[Route("api/sessions")]
[Produces("application/json")]
public class SessionsController : ControllerBase
{
    private readonly IWorkoutSessionService _svc;

    public SessionsController(IWorkoutSessionService svc)
        => _svc = svc;

    /// <summary>
    /// Startuje sesję na podstawie zaplanowanego treningu (ScheduledWorkout).
    /// </summary>
    /// <remarks>
    /// Tworzy snapshot ćwiczeń i serii z planu. Zwraca aktualny stan sesji.
    /// </remarks>
    /// <response code="201">Sesja została utworzona i wystartowana.</response>
    /// <response code="400">Nieprawidłowe dane wejściowe.</response>
    /// <response code="401">Brak autoryzacji.</response>
    /// <response code="404">Zaplanowany trening nie istnieje.</response>
    /// <response code="500">Nieoczekiwany błąd serwera.</response>
    [HttpPost("start")]
    [ProducesResponseType(typeof(WorkoutSessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WorkoutSessionDto>> Start(
        [FromBody] StartSessionRequest request,
        CancellationToken ct)
    {
        // [ApiController] + DataAnnotations/IValidatableObject zrobią tu 400 automatycznie,
        // więc nie trzeba ręcznie sprawdzać ModelState.

        var dto = await _svc.StartAsync(request, ct);
        // bardziej REST-owo: 201 + Location: /api/sessions/{id}
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Częściowo aktualizuje wyniki pojedynczej serii w trakcie sesji.
    /// </summary>
    /// <remarks>
    /// Używane przez UI przy zapisie wyników (reps/weight/RPE/failure) dla danej serii.
    /// </remarks>
    /// <response code="200">Zwraca zaktualizowany stan całej sesji.</response>
    /// <response code="400">Nieprawidłowe dane lub sesja nie jest w statusie in_progress.</response>
    /// <response code="401">Brak autoryzacji.</response>
    /// <response code="404">Sesja, ćwiczenie lub seria nie istnieje.</response>
    [HttpPatch("{id:guid}/set")]
    [ProducesResponseType(typeof(WorkoutSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WorkoutSessionDto>> PatchSet(
        Guid id,
        [FromBody] PatchSetRequest req,
        CancellationToken ct)
    {
        var dto = await _svc.PatchSetAsync(id, req, ct);
        return Ok(dto);
    }

    /// <summary>
    /// Kończy sesję treningową.
    /// </summary>
    /// <remarks>
    /// Ustawia status na <c>completed</c>, wylicza czas trwania oraz zapisuje notatki z sesji.
    /// </remarks>
    /// <response code="200">Sesja została zakończona.</response>
    /// <response code="400">Sesja nie jest w statusie in_progress.</response>
    /// <response code="401">Brak autoryzacji.</response>
    /// <response code="404">Sesja nie istnieje.</response>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(WorkoutSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WorkoutSessionDto>> Complete(
        Guid id,
        [FromBody] CompleteSessionRequest req,
        CancellationToken ct)
    {
        var dto = await _svc.CompleteAsync(id, req, ct);
        return Ok(dto);
    }

    /// <summary>
    /// Przerywa sesję treningową.
    /// </summary>
    /// <remarks>
    /// Ustawia status na <c>aborted</c> i zapisuje powód przerwania w notatkach sesji.
    /// </remarks>
    /// <response code="200">Sesja została przerwana.</response>
    /// <response code="400">Sesja nie jest w statusie in_progress.</response>
    /// <response code="401">Brak autoryzacji.</response>
    /// <response code="404">Sesja nie istnieje.</response>
    [HttpPost("{id:guid}/abort")]
    [ProducesResponseType(typeof(WorkoutSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WorkoutSessionDto>> Abort(
        Guid id,
        [FromBody] AbortSessionRequest req,
        CancellationToken ct)
    {
        var dto = await _svc.AbortAsync(id, req, ct);
        return Ok(dto);
    }

    /// <summary>
    /// Pobiera pojedynczą sesję po Id.
    /// </summary>
    /// <response code="200">Zwraca sesję treningową.</response>
    /// <response code="401">Brak autoryzacji.</response>
    /// <response code="404">Sesja nie istnieje.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkoutSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WorkoutSessionDto>> GetById(
        Guid id,
        CancellationToken ct)
    {
        var dto = await _svc.GetByIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>
    /// Zwraca listę sesji w zadanym przedziale czasu (UTC).
    /// </summary>
    /// <remarks>
    /// Parametry <c>fromUtc</c> i <c>toUtc</c> powinny być w formacie ISO 8601, np.
    /// <c>2025-11-01T00:00:00Z</c>.
    /// </remarks>
    /// <response code="200">Zwraca listę sesji w zakresie.</response>
    /// <response code="400">Nieprawidłowy format daty lub zakres (toUtc &lt;= fromUtc).</response>
    /// <response code="401">Brak autoryzacji.</response>
    [HttpGet("by-range")]
    [ProducesResponseType(typeof(IReadOnlyList<WorkoutSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<WorkoutSessionDto>>> ByRange(
        [FromQuery] SessionsByRangeRequest query,
        CancellationToken ct)
    {
        var (fromUtc, toUtc) = query.NormalizeToUtc();

        var list = await _svc.GetByRangeAsync(fromUtc, toUtc, ct);
        return Ok(list);
    }
}