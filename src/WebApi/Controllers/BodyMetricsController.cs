using Application.Abstractions;
using Application.DTOs.BodyMetrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Zarządzanie pomiarami ciała (waga, wzrost, BMI).
/// </summary>
[ApiController]
[Route("api/body-metrics")]
[Authorize]
public class BodyMetricsController : ControllerBase
{
    private readonly IBodyMetricsService _service;

    public BodyMetricsController(IBodyMetricsService service)
    {
        _service = service;
    }

    /// <summary>
    /// Dodaje nowy pomiar ciała.
    /// </summary>
    /// <remarks>
    /// Rejestruje nowy pomiar wagi, wzrostu i innych wymiarów.
    /// BMI jest obliczane automatycznie na podstawie wzrostu i wagi.
    /// 
    /// **Wymagania:**
    /// * `WeightKg`: Wartość dodatnia (np. 80.5).
    /// * `HeightCm`: Wartość dodatnia (np. 180).
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     POST /api/body-metrics
    ///     {
    ///       "weightKg": 82.5,
    ///       "heightCm": 178,
    ///       "bodyFatPercentage": 18.5,
    ///       "notes": "Po treningu"
    ///     }
    /// </remarks>
    /// <param name="dto">Dane nowego pomiaru.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Utworzony pomiar.</returns>
    /// <response code="201">Pomiar został pomyślnie dodany.</response>
    /// <response code="400">Błąd walidacji danych.</response>
    [HttpPost]
    [ProducesResponseType(typeof(BodyMeasurementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BodyMeasurementDto>> AddMeasurement(
        [FromBody] CreateBodyMeasurementDto dto,
        CancellationToken ct)
    {
        var result = await _service.AddMeasurementAsync(dto, ct);
        return CreatedAtAction(nameof(GetLatest), new { }, result);
    }

    /// <summary>
    /// Pobiera historię pomiarów w zadanym zakresie dat.
    /// </summary>
    /// <remarks>
    /// Zwraca listę pomiarów posortowaną malejąco po dacie (najnowsze pierwsze).
    /// Jeśli nie podano dat, zwraca wszystkie dostępne pomiary.
    /// </remarks>
    /// <param name="from">Data początkowa (opcjonalna).</param>
    /// <param name="to">Data końcowa (opcjonalna).</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Lista pomiarów.</returns>
    /// <response code="200">Lista pomiarów (może być pusta).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BodyMeasurementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BodyMeasurementDto>>> GetMeasurements(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var measurements = await _service.GetMeasurementsAsync(from, to, ct);
        return Ok(measurements);
    }

    /// <summary>
    /// Pobiera najnowszy zarejestrowany pomiar.
    /// </summary>
    /// <remarks>
    /// Przydatne do szybkiego sprawdzenia aktualnej wagi i BMI użytkownika.
    /// </remarks>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Najnowszy pomiar lub 404 jeśli brak pomiarów.</returns>
    /// <response code="200">Najnowszy pomiar.</response>
    /// <response code="404">Użytkownik nie posiada żadnych pomiarów.</response>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(BodyMeasurementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BodyMeasurementDto>> GetLatest(CancellationToken ct)
    {
        var result = await _service.GetLatestMeasurementAsync(ct);
        return result != null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Pobiera podsumowanie statystyk pomiarów.
    /// </summary>
    /// <remarks>
    /// Zwraca zagregowane dane, takie jak:
    /// * Aktualna waga i BMI.
    /// * Najwyższa i najniższa zanotowana waga.
    /// * Całkowita liczba pomiarów.
    /// * Kategoria BMI (np. "Normal", "Overweight").
    /// </remarks>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Obiekt ze statystykami.</returns>
    /// <response code="200">Statystyki pomiarów.</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(BodyMetricsStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BodyMetricsStatsDto>> GetStats(CancellationToken ct)
    {
        var stats = await _service.GetStatsAsync(ct);
        return Ok(stats);
    }

    /// <summary>
    /// Pobiera dane do wykresu progresji w czasie.
    /// </summary>
    /// <remarks>
    /// Zwraca uproszczoną listę punktów danych (data, waga, BMI) z zadanego okresu,
    /// idealną do rysowania wykresów liniowych postępu.
    /// </remarks>
    /// <param name="from">Początek okresu wykresu.</param>
    /// <param name="to">Koniec okresu wykresu.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Lista punktów danych wykresu.</returns>
    /// <response code="200">Dane progresji.</response>
    [HttpGet("progress")]
    [ProducesResponseType(typeof(IReadOnlyList<BodyMetricsProgressDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BodyMetricsProgressDto>>> GetProgress(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
    {
        var progress = await _service.GetProgressAsync(from, to, ct);
        return Ok(progress);
    }

    /// <summary>
    /// Usuwa pojedynczy wpis pomiaru.
    /// </summary>
    /// <remarks>
    /// Trwale usuwa wskazany pomiar z historii użytkownika.
    /// Operacja jest nieodwracalna.
    /// </remarks>
    /// <param name="id">Identyfikator pomiaru (GUID).</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="204">Pomiar został usunięty.</response>
    /// <response code="404">Pomiar nie istnieje lub nie należy do użytkownika.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMeasurement(Guid id, CancellationToken ct)
    {
        await _service.DeleteMeasurementAsync(id, ct);
        return NoContent();
    }
}
