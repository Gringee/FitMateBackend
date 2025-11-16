using Application.Abstractions;
using Application.DTOs.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// API do analityki treningowej (przegląd, wolumen, e1RM, adherence, plan vs actual).
/// </summary>
[Authorize]
[ApiController]
[Route("api/analytics")]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _svc;

    public AnalyticsController(IAnalyticsService svc) => _svc = svc;

    /// <summary>
    /// Zwraca ogólny przegląd metryk w danym zakresie czasu (UTC).
    /// </summary>
    /// <remarks>
    /// Zakres określany jest parametrami <c>from</c> i <c>to</c> (UTC, ISO 8601).
    /// </remarks>
    /// <response code="200">Zwraca overview w zadanym zakresie.</response>
    /// <response code="400">Nieprawidłowy zakres dat.</response>
    /// <response code="401">Brak autoryzacji.</response>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(OverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OverviewDto>> Overview(
        [FromQuery] OverviewQueryDto query,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var (fromUtc, toUtc) = query.ToUtcRange();
        var dto = await _svc.GetOverviewAsync(fromUtc, toUtc, ct);
        return Ok(dto);
    }

    /// <summary>
    /// Zwraca wolumen treningowy w czasie (sumaryczny lub per ćwiczenie).
    /// </summary>
    /// <remarks>
    /// Parametry:
    /// <list type="bullet">
    /// <item><c>from</c>, <c>to</c> – zakres czasu (UTC)</item>
    /// <item><c>groupBy</c> – "day", "week" lub "exercise"</item>
    /// <item><c>exerciseName</c> – opcjonalny filtr po nazwie ćwiczenia</item>
    /// </list>
    /// </remarks>
    /// <response code="200">Zwraca listę punktów czasowych.</response>
    /// <response code="400">Nieprawidłowe parametry (zakres czasu lub groupBy).</response>
    /// <response code="401">Brak autoryzacji.</response>
    [HttpGet("volume")]
    [ProducesResponseType(typeof(IReadOnlyList<TimePointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<TimePointDto>>> Volume(
        [FromQuery] VolumeQueryDto query,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var (fromUtc, toUtc) = query.ToUtcRange();
        var groupBy = query.GroupByNormalized;

        var list = await _svc.GetVolumeAsync(fromUtc, toUtc, groupBy, query.ExerciseName, ct);
        return Ok(list);
    }

    /// <summary>
    /// Zwraca historię szacowanego 1RM (e1RM) dla konkretnego ćwiczenia.
    /// </summary>
    /// <remarks>
    /// Nazwa ćwiczenia przekazywana jest w ścieżce (<c>/exercises/{name}/e1rm</c>),
    /// a zakres czasu w query paramach <c>from</c> i <c>to</c> (UTC).
    /// </remarks>
    /// <response code="200">Zwraca listę punktów e1RM.</response>
    /// <response code="400">Nieprawidłowy zakres czasu.</response>
    /// <response code="401">Brak autoryzacji.</response>
    [HttpGet("exercises/{name}/e1rm")]
    [ProducesResponseType(typeof(IReadOnlyList<E1rmPointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<E1rmPointDto>>> E1rm(
        [FromRoute] string name,
        [FromQuery] E1rmQueryDto query,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Exercise name is required.",
                Detail = "Route parameter 'name' cannot be empty.",
                Instance = HttpContext.Request.Path
            });

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var (fromUtc, toUtc) = query.ToUtcRange();
        var list = await _svc.GetE1RmAsync(name, fromUtc, toUtc, ct);
        return Ok(list);
    }

    /// <summary>
    /// Zwraca adherence (wykonane vs zaplanowane treningi) w zadanym zakresie dni.
    /// </summary>
    /// <remarks>
    /// <c>fromDate</c> i <c>toDate</c> są datami bez czasu. 
    /// W implementacji <c>toDate</c> traktowane jest jako wartość górna zakresu (exclusive).
    /// </remarks>
    /// <response code="200">Zwraca statystyki adherence.</response>
    /// <response code="400">Nieprawidłowy zakres dat.</response>
    /// <response code="401">Brak autoryzacji.</response>
    [HttpGet("adherence")]
    [ProducesResponseType(typeof(AdherenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AdherenceDto>> Adherence(
        [FromQuery] AdherenceQueryDto query,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var (from, to) = query.ToRange();
        var dto = await _svc.GetAdherenceAsync(from, to, ct);
        return Ok(dto);
    }

    /// <summary>
    /// Zwraca porównanie planu do wykonania (Plan vs Actual) dla konkretnej sesji.
    /// </summary>
    /// <remarks>
    /// Wymaga podania <c>sessionId</c> w query stringu.
    /// </remarks>
    /// <response code="200">Zwraca listę serii z różnicami plan vs actual.</response>
    /// <response code="400">Nieprawidłowy <c>sessionId</c>.</response>
    /// <response code="401">Brak autoryzacji.</response>
    [HttpGet("plan-vs-actual")]
    [ProducesResponseType(typeof(IReadOnlyList<PlanVsActualItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<PlanVsActualItemDto>>> PlanVsActual(
        [FromQuery] PlanVsActualQueryDto query,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var items = await _svc.GetPlanVsActualAsync(query.SessionId, ct);
        return Ok(items);
    }
}