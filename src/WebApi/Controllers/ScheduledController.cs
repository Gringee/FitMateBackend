using Application.Abstractions;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Zarządzanie zaplanowanymi treningami (kalendarz).
/// </summary>
/// <remarks>
/// Wszystkie operacje są wykonywane w kontekście zalogowanego użytkownika
/// (na podstawie identyfikatora z tokenu JWT).
/// </remarks>
[ApiController]
[Route("api/scheduled")]
[Authorize]
public class ScheduledController : ControllerBase
{
    private readonly IScheduledService _svc;

    public ScheduledController(IScheduledService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// Tworzy nowe zaplanowane wydarzenie treningowe na wybrany dzień (i opcjonalnie godzinę).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Jeśli w <see cref="CreateScheduledDto.Exercises"/> nie zostaną podane ćwiczenia,
    /// zostaną skopiowane z przypisanego planu.
    /// </para>
    /// <para>
    /// Format daty: <c>yyyy-MM-dd</c>, format czasu (opcjonalny): <c>HH:mm</c>.
    /// </para>
    /// </remarks>
    /// <param name="dto">Dane nowego zaplanowanego treningu.</param>
    /// <response code="201">Utworzony rekord zaplanowanego treningu.</response>
    /// <response code="400">Niepoprawne dane wejściowe (np. błędny format daty, brak wymaganych pól).</response>
    [HttpPost]
    [ProducesResponseType(typeof(ScheduledDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScheduledDto>> Create(
        [FromBody] CreateScheduledDto dto,
        CancellationToken ct)
    {
        var res = await _svc.CreateAsync(dto, ct);
        return CreatedAtRoute("GetScheduledById", new { id = res.Id }, res);
    }

    /// <summary>
    /// Zwraca wszystkie zaplanowane treningi zalogowanego użytkownika.
    /// </summary>
    /// <remarks>
    /// Wyniki są sortowane rosnąco po dacie i godzinie.
    /// </remarks>
    /// <response code="200">Lista zaplanowanych treningów.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<ScheduledDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ScheduledDto>>> GetAll(CancellationToken ct)
    {
        var items = await _svc.GetAllAsync(ct);
        return Ok(items);
    }

    /// <summary>
    /// Zwraca pojedynczy zaplanowany trening po jego Id.
    /// </summary>
    /// <param name="id">Identyfikator zaplanowanego treningu.</param>
    /// <response code="200">Znaleziony trening.</response>
    /// <response code="404">Nie znaleziono treningu o podanym Id (lub nie należy do użytkownika).</response>
    [HttpGet("{id:guid}", Name = "GetScheduledById")]
    [ProducesResponseType(typeof(ScheduledDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduledDto>> GetById(Guid id, CancellationToken ct)
    {
        var res = await _svc.GetByIdAsync(id, ct);
        return res is not null ? Ok(res) : NotFound();
    }

    /// <summary>
    /// Zwraca zaplanowane treningi dla konkretnej daty.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Parametr <c>date</c> musi mieć format <c>yyyy-MM-dd</c>, np. <c>2025-11-15</c>.
    /// </para>
    /// </remarks>
    /// <param name="date">Data dzienna w formacie <c>yyyy-MM-dd</c>.</param>
    /// <response code="200">Lista treningów w danym dniu (może być pusta).</response>
    /// <response code="400">Błędny format daty.</response>
    [HttpGet("by-date")]
    [ProducesResponseType(typeof(List<ScheduledDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ScheduledDto>>> GetByDate(
        [FromQuery] string date,
        CancellationToken ct)
    {
        var items = await _svc.GetByDateAsync(date, ct);
        return Ok(items);
    }

    /// <summary>
    /// Aktualizuje istniejący zaplanowany trening.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Podczas aktualizacji istniejące ćwiczenia i serie są usuwane i zastępowane
    /// nowym zestawem z <see cref="CreateScheduledDto.Exercises"/>.
    /// </para>
    /// </remarks>
    /// <param name="id">Identyfikator zaplanowanego treningu.</param>
    /// <param name="dto">Nowe dane zaplanowanego treningu.</param>
    /// <response code="200">Zaktualizowany trening.</response>
    /// <response code="404">Trening nie istnieje lub nie należy do użytkownika.</response>
    /// <response code="400">Niepoprawne dane wejściowe.</response>
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
    /// Usuwa zaplanowany trening.
    /// </summary>
    /// <param name="id">Identyfikator zaplanowanego treningu.</param>
    /// <response code="204">Trening został usunięty.</response>
    /// <response code="404">Trening nie istnieje lub nie należy do użytkownika.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Duplikuje istniejący zaplanowany trening.
    /// </summary>
    /// <remarks>
    /// Tworzy nowy wpis z tym samym planem, ćwiczeniami, datą i godziną,
    /// ale z nowym identyfikatorem.
    /// </remarks>
    /// <param name="id">Id zaplanowanego treningu do zduplikowania.</param>
    /// <response code="200">Nowy, zduplikowany trening.</response>
    /// <response code="404">Oryginalny trening nie istnieje lub nie należy do użytkownika.</response>
    [HttpPost("{id:guid}/duplicate")]
    [ProducesResponseType(typeof(ScheduledDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduledDto>> Duplicate(Guid id, CancellationToken ct)
    {
        var res = await _svc.DuplicateAsync(id, ct);
        return res is not null ? Ok(res) : NotFound();
    }
}