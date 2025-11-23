using Application.Abstractions;
using Application.DTOs.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace WebApi.Controllers;

/// <summary>
/// Analityka i statystyki treningowe.
/// </summary>
/// <remarks>
/// Kontroler dostarcza dane agregujące historię treningową użytkownika.
/// Pozwala śledzić postępy (objętość, siła), konsystencję (adherence) oraz szczegółową analizę wykonania planu.
/// </remarks>
[Authorize]
[ApiController]
[Route("api/analytics")]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _svc;

    public AnalyticsController(IAnalyticsService svc) => _svc = svc;

    /// <summary>
    /// Pobiera ogólny przegląd statystyk (Dashboard).
    /// </summary>
    /// <remarks>
    /// Zwraca podsumowanie dla zadanego okresu: całkowita objętość, średnia intensywność, liczba sesji i % realizacji planu.
    /// 
    /// **Wymagany format dat:** ISO 8601 UTC (np. <c>2026-11-01T00:00:00Z</c>).
    /// </remarks>
    /// <param name="query">Parametry zakresu czasu (From, To).</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Obiekt z podsumowaniem statystyk.</returns>
    /// <response code="200">Statystyki zostały obliczone.</response>
    /// <response code="400">Błędny zakres dat (np. To &lt; From).</response>
    /// <response code="401">Użytkownik niezalogowany.</response>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(OverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
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
    /// Analiza objętości treningowej (Volume).
    /// </summary>
    /// <remarks>
    /// Zwraca dane wykresu objętości (Volume Load = Series * Reps * Weight).
    /// 
    /// **Opcje grupowania (`groupBy`):**
    /// * <c>day</c> – Suma objętości na każdy dzień treningowy.
    /// * <c>week</c> – Suma objętości na tydzień (format ISO Week, np. "2025-W45").
    /// * <c>exercise</c> – Suma objętości per ćwiczenie w całym zadanym okresie.
    /// 
    /// **Filtrowanie:**
    /// Możesz ograniczyć wynik do konkretnego ćwiczenia podając <c>exerciseName</c>.
    /// </remarks>
    /// <param name="query">Parametry zapytania (zakres dat, grupowanie, filtr ćwiczenia).</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Lista punktów danych do wykresu.</returns>
    /// <response code="200">Dane objętości.</response>
    /// <response code="400">Nieprawidłowy parametr <c>groupBy</c> lub zakres dat.</response>
    [HttpGet("volume")]
    [ProducesResponseType(typeof(IReadOnlyList<TimePointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
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
    /// Historia szacowanego rekordu na 1 powtórzenie (e1RM).
    /// </summary>
    /// <remarks>
    /// Oblicza e1RM (Estimated 1 Rep Max) dla każdego treningu danego ćwiczenia w zadanym okresie.
    /// 
    /// **Algorytm:**
    /// Wykorzystywana jest formuła **Epleya**: <c>Weight * (1 + Reps / 30)</c>.
    /// 
    /// **Przykład użycia:**
    /// 
    ///     GET /api/analytics/exercises/Bench%20Press/e1rm?from=...&amp;to=...
    ///     
    /// </remarks>
    /// <param name="name">Nazwa ćwiczenia (np. "Bench Press").</param>
    /// <param name="query">Zakres czasu (From, To).</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Lista punktów e1RM w czasie.</returns>
    /// <response code="200">Historia e1RM.</response>
    /// <response code="400">Nie podano nazwy ćwiczenia lub błędny zakres dat.</response>
    [HttpGet("exercises/{name}/e1rm")]
    [ProducesResponseType(typeof(IReadOnlyList<E1rmPointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
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
    /// Statystyki konsekwencji treningowej (Adherence).
    /// </summary>
    /// <remarks>
    /// Porównuje liczbę zaplanowanych treningów do liczby rzeczywiście zrealizowanych (Completed) w zadanym okresie.
    /// 
    /// **Ważne:** Parametry daty są tu typu <c>DateOnly</c> (format: <c>yyyy-MM-dd</c>), a nie DateTime UTC.
    /// </remarks>
    /// <param name="query">Zakres dat (FromDate, ToDate).</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Obiekt adherence (Planned, Completed, Percentage).</returns>
    /// <response code="200">Statystyki adherence.</response>
    /// <response code="400">Data końcowa jest wcześniejsza niż początkowa.</response>
    [HttpGet("adherence")]
    [ProducesResponseType(typeof(AdherenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
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
    /// Porównanie Plan vs Wykonanie dla konkretnej sesji.
    /// </summary>
    /// <remarks>
    /// Zwraca szczegółową listę serii z danej sesji, pokazując różnice między założeniami (Planned)
    /// a rzeczywistym wykonaniem (Done) dla ciężaru i powtórzeń.
    /// </remarks>
    /// <param name="query">Zapytanie zawierające <c>sessionId</c>.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Lista detali serii z obliczonymi różnicami (Diff).</returns>
    /// <response code="200">Dane porównawcze.</response>
    /// <response code="400">Nieprawidłowy identyfikator sesji.</response>
    [HttpGet("plan-vs-actual")]
    [ProducesResponseType(typeof(IReadOnlyList<PlanVsActualItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
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