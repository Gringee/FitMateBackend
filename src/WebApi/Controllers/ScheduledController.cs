using Application.Abstractions;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace WebApi.Controllers;

/// <summary>
/// Zarządzanie zaplanowanymi treningami (Kalendarz).
/// </summary>
/// <remarks>
/// Kontroler umożliwia planowanie treningów na konkretne dni, oznaczanie ich jako wykonane
/// oraz zarządzanie historią aktywności.
/// Wszystkie operacje są wykonywane w kontekście zalogowanego użytkownika.
/// </remarks>
[ApiController]
[Route("api/scheduled")]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class ScheduledController : ControllerBase
{
    private readonly IScheduledService _svc;

    public ScheduledController(IScheduledService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// Planuje nowy trening w kalendarzu.
    /// </summary>
    /// <remarks>
    /// Tworzy wpis w kalendarzu na podstawie istniejącego `PlanId`.
    /// 
    /// **Logika działania:**
    /// * Jeśli lista `Exercises` jest pusta (lub null), system automatycznie skopiuje ćwiczenia z planu bazowego (`PlanId`).
    /// * Jeśli przekażesz `Exercises`, nadpiszą one domyślny zestaw z planu (pozwala to modyfikować trening tylko na ten jeden dzień).
    /// 
    /// **Formatowanie:**
    /// * `Date`: Wymagany format <c>yyyy-MM-dd</c> (np. "2026-11-15").
    /// * `Time`: Opcjonalny format <c>HH:mm</c> (np. "18:30").
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     POST /api/scheduled
    ///     {
    ///       "date": "2026-11-15",
    ///       "time": "18:00",
    ///       "planId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "status": "planned",
    ///       "visibleToFriends": true
    ///     }
    /// </remarks>
    /// <param name="dto">Obiekt tworzenia zaplanowanego treningu.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Utworzony obiekt zaplanowanego treningu.</returns>
    /// <response code="201">Trening został pomyślnie zaplanowany.</response>
    /// <response code="400">
    /// Błąd walidacji danych (np. zły format daty) lub status inny niż 'planned'/'completed'.
    /// </response>
    /// <response code="404">Podany `PlanId` nie istnieje lub nie należy do użytkownika.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ScheduledDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduledDto>> Create(
        [FromBody] CreateScheduledDto dto,
        CancellationToken ct)
    {
        // Service rzuca KeyNotFoundException jeśli PlanId nie istnieje -> Middleware 404
        var res = await _svc.CreateAsync(dto, ct);
        return CreatedAtRoute("GetScheduledById", new { id = res.Id }, res);
    }

    /// <summary>
    /// Pobiera listę wszystkich zaplanowanych treningów użytkownika.
    /// </summary>
    /// <remarks>
    /// Zwraca pełną listę treningów (historię oraz przyszłe plany), posortowaną chronologicznie (Data -> Czas).
    /// </remarks>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="200">Lista treningów.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ScheduledDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ScheduledDto>>> GetAll(CancellationToken ct)
    {
        var items = await _svc.GetAllAsync(ct);
        return Ok(items);
    }

    /// <summary>
    /// Pobiera szczegóły konkretnego treningu z kalendarza.
    /// </summary>
    /// <param name="id">Identyfikator zaplanowanego treningu (GUID).</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="200">Szczegóły treningu (wraz z ćwiczeniami i seriami).</response>
    /// <response code="404">Trening nie istnieje lub nie należy do zalogowanego użytkownika.</response>
    [HttpGet("{id:guid}", Name = "GetScheduledById")]
    [ProducesResponseType(typeof(ScheduledDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduledDto>> GetById(Guid id, CancellationToken ct)
    {
        var res = await _svc.GetByIdAsync(id, ct);
        return res is not null ? Ok(res) : NotFound();
    }

    /// <summary>
    /// Pobiera treningi zaplanowane na konkretny dzień.
    /// </summary>
    /// <remarks>
    /// Filtruje listę treningów po dacie.
    /// 
    /// **Przykładowe użycie:**
    /// 
    ///     GET /api/scheduled/by-date?date=2026-10-25
    ///     
    /// </remarks>
    /// <param name="date">Data w formacie <c>yyyy-MM-dd</c>.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Lista treningów w danym dniu.</returns>
    /// <response code="200">Lista treningów (może być pusta).</response>
    /// <response code="400">Podano datę w nieprawidłowym formacie.</response>
    [HttpGet("by-date")]
    [ProducesResponseType(typeof(IReadOnlyList<ScheduledDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ScheduledDto>>> GetByDate(
        [FromQuery] DateOnly date,
        CancellationToken ct)
    {
        var items = await _svc.GetByDateAsync(date, ct);
        return Ok(items);
    }

    /// <summary>
    /// Aktualizuje istniejący trening w kalendarzu.
    /// </summary>
    /// <remarks>
    /// Pozwala zmienić datę, godzinę, notatki, status (np. na "completed") lub zmodyfikować listę ćwiczeń.
    /// 
    /// **Uwaga:** Przesłanie nowej listy `Exercises` całkowicie zastępuje starą listę ćwiczeń i serii dla tego wpisu.
    /// </remarks>
    /// <param name="id">Identyfikator edytowanego wpisu.</param>
    /// <param name="dto">Nowe dane treningu.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="200">Zaktualizowany trening.</response>
    /// <response code="400">Błąd walidacji (np. format daty).</response>
    /// <response code="404">Wpis nie istnieje lub plan bazowy (PlanId) został usunięty.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ScheduledDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScheduledDto>> Update(
        Guid id,
        [FromBody] CreateScheduledDto dto,
        CancellationToken ct)
    {
        var res = await _svc.UpdateAsync(id, dto, ct);
        return res is not null ? Ok(res) : NotFound();
    }

    /// <summary>
    /// Usuwa zaplanowany trening z kalendarza.
    /// </summary>
    /// <remarks>
    /// Operacja jest nieodwracalna. Usuwa wpis wraz z przypisanymi do niego seriami/ćwiczeniami (Cascade Delete).
    /// </remarks>
    /// <param name="id">Identyfikator wpisu do usunięcia.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="204">Wpis został usunięty.</response>
    /// <response code="404">Wpis nie istnieje lub nie należy do użytkownika.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Duplikuje istniejący wpis w kalendarzu.
    /// </summary>
    /// <remarks>
    /// Duplikuje zaplanowany trening (taka sama data, czas i ćwiczenia), ale z nowym ID.
    /// Przydatne np. gdy użytkownik chce zrobić ten sam trening dwa razy dziennie lub szybko skopiować ustawienia.
    /// </remarks>
    /// <param name="id">Identyfikator wpisu źródłowego.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Nowo utworzony obiekt (kopia).</returns>
    /// <response code="201">Kopia została utworzona pomyślnie.</response>
    /// <response code="404">Wpis źródłowy nie istnieje.</response>
    [HttpPost("{id:guid}/duplicate")]
    [ProducesResponseType(typeof(ScheduledDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduledDto>> Duplicate(Guid id, CancellationToken ct)
    {
        var res = await _svc.DuplicateAsync(id, ct);
        if (res is null) return NotFound();
        
        return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
    }
}