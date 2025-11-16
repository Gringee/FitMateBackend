using Application.Abstractions;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/plans")]
public class PlansController : ControllerBase
{
    private readonly IPlanService _svc;

    public PlansController(IPlanService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// Tworzy nowy plan treningowy dla zalogowanego użytkownika.
    /// </summary>
    /// <remarks>
    /// Plan jest przypisywany do aktualnie zalogowanego użytkownika
    /// (na podstawie <c>userId</c> z tokenu JWT).
    /// </remarks>
    /// <param name="dto">Dane planu (nazwa, typ, ćwiczenia).</param>
    /// <response code="200">Plan został utworzony.</response>
    /// <response code="400">Niepoprawne dane wejściowe (walidacja DTO).</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlanDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlanDto>> Create(
        [FromBody] CreatePlanDto dto,
        CancellationToken ct)
    {
        var result = await _svc.CreateAsync(dto, ct);
        return Ok(result);
    }

    /// <summary>
    /// Pobiera wszystkie plany utworzone przez zalogowanego użytkownika.
    /// </summary>
    /// <response code="200">Lista planów użytkownika.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PlanDto>))]
    public async Task<ActionResult<List<PlanDto>>> GetAll(CancellationToken ct)
    {
        var result = await _svc.GetAllAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Pobiera pojedynczy plan po identyfikatorze.
    /// </summary>
    /// <remarks>
    /// Zwraca tylko plan, który należy do zalogowanego użytkownika.
    /// </remarks>
    /// <param name="id">Identyfikator planu.</param>
    /// <response code="200">Znaleziony plan.</response>
    /// <response code="404">Plan nie istnieje lub nie należy do użytkownika.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlanDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var res = await _svc.GetByIdAsync(id, ct);
        return res is not null ? Ok(res) : NotFound();
    }

    /// <summary>
    /// Aktualizuje istniejący plan treningowy.
    /// </summary>
    /// <remarks>
    /// Możliwa jest edycja wyłącznie planów utworzonych przez zalogowanego użytkownika.
    /// </remarks>
    /// <param name="id">Identyfikator planu.</param>
    /// <param name="dto">Nowe dane planu.</param>
    /// <response code="200">Plan został zaktualizowany.</response>
    /// <response code="404">Plan nie istnieje lub nie należy do użytkownika.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlanDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreatePlanDto dto, CancellationToken ct)
    {
        var res = await _svc.UpdateAsync(id, dto, ct);
        return res is not null ? Ok(res) : NotFound();
    }

    /// <summary>
    /// Usuwa plan treningowy.
    /// </summary>
    /// <remarks>
    /// Usunąć można tylko plan należący do zalogowanego użytkownika.
    /// </remarks>
    /// <param name="id">Identyfikator planu.</param>
    /// <response code="204">Plan został usunięty.</response>
    /// <response code="404">Plan nie istnieje lub nie należy do użytkownika.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Duplikuje istniejący plan użytkownika.
    /// </summary>
    /// <remarks>
    /// Tworzy kopię planu z dopiskiem <c>(Copy)</c> w nazwie.
    /// </remarks>
    /// <param name="id">Identyfikator planu źródłowego.</param>
    /// <response code="200">Nowo utworzony, zduplikowany plan.</response>
    /// <response code="404">Plan nie istnieje lub nie należy do użytkownika.</response>
    [HttpPost("{id:guid}/duplicate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlanDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken ct)
    {
        var res = await _svc.DuplicateAsync(id, ct);
        return res is not null ? Ok(res) : NotFound();
    }

    /// <summary>
    /// Udostępnia plan innemu użytkownikowi.
    /// </summary>
    /// <remarks>
    /// Użytkownik docelowy wskazywany jest przez jego <c>userId</c>.
    /// Udostępnić można tylko własne plany.
    /// </remarks>
    /// <param name="planId">Id planu do udostępnienia.</param>
    /// <param name="targetUserId">Id użytkownika, któremu plan jest udostępniany.</param>
    /// <response code="200">Plan został udostępniony.</response>
    /// <response code="400">Plan już był udostępniony lub inne błędy biznesowe.</response>
    /// <response code="404">Plan lub użytkownik docelowy nie istnieje.</response>
    [HttpPost("{planId:guid}/share-to/{targetUserId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ShareToUser(Guid planId, Guid targetUserId, CancellationToken ct)
    {
        await _svc.ShareToUserAsync(planId, targetUserId, ct);
        return Ok(new { message = "Plan shared successfully." });
    }

    /// <summary>
    /// Zwraca listę planów, które zostały zaakceptowane i są dostępne dla zalogowanego użytkownika.
    /// </summary>
    /// <response code="200">Lista planów udostępnionych użytkownikowi.</response>
    [HttpGet("shared-with-me")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PlanDto>))]
    public async Task<ActionResult<List<PlanDto>>> GetSharedWithMe(CancellationToken ct)
    {
        var result = await _svc.GetSharedWithMeAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Zwraca oczekujące zaproszenia do współdzielenia planów.
    /// </summary>
    /// <remarks>
    /// Endpoint pokazuje plany, które zostały udostępnione zalogowanemu użytkownikowi
    /// i czekają na akceptację lub odrzucenie.
    /// </remarks>
    /// <response code="200">Lista oczekujących udostępnień.</response>
    [HttpGet("shared/pending")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SharedPlanDto>))]
    public async Task<ActionResult<List<SharedPlanDto>>> GetPendingSharedPlans(CancellationToken ct)
    {
        var items = await _svc.GetPendingSharedPlansAsync(ct);
        return Ok(items);
    }

    /// <summary>
    /// Akceptuje lub odrzuca udostępniony plan.
    /// </summary>
    /// <remarks>
    /// Użytkownik, któremu plan został udostępniony, może zaakceptować lub odrzucić
    /// zaproszenie, zmieniając jego status z <c>Pending</c> na <c>Accepted</c> lub <c>Rejected</c>.
    /// </remarks>
    /// <param name="sharedPlanId">Identyfikator udostępnienia.</param>
    /// <param name="body">Informacja, czy zaakceptować (<c>true</c>) czy odrzucić.</param>
    /// <response code="200">Plan został zaakceptowany lub odrzucony.</response>
    /// <response code="400">Zaproszenie już zostało rozpatrzone lub inne błędy biznesowe.</response>
    /// <response code="404">Udostępnienie nie istnieje albo nie należy do użytkownika.</response>
    [HttpPost("shared/{sharedPlanId:guid}/respond")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RespondToSharedPlan(
        Guid sharedPlanId,
        [FromBody] RespondSharedPlanRequest body,
        CancellationToken ct)
    {
        await _svc.RespondToSharedPlanAsync(sharedPlanId, body.Accept, ct);
        return Ok(new { message = body.Accept ? "Plan accepted." : "Plan rejected." });
    }

    /// <summary>
    /// Zwraca historię zaakceptowanych i odrzuconych udostępnień planów.
    /// </summary>
    /// <response code="200">
    /// Lista udostępnień ze statusem innym niż <c>Pending</c> (np. <c>Accepted</c>, <c>Rejected</c>).
    /// </response>
    [HttpGet("shared/history")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SharedPlanDto>))]
    public async Task<ActionResult<List<SharedPlanDto>>> GetSharedHistory(CancellationToken ct)
    {
        var items = await _svc.GetSharedHistoryAsync(ct);
        return Ok(items);
    }

    /// <summary>
    /// Cofnięcie udostępnienia planu przez autora.
    /// </summary>
    /// <remarks>
    /// Usuwa wpis udostępnienia – użytkownik docelowy traci dostęp do planu.
    /// Możliwe tylko dla właściciela udostępnienia.
    /// </remarks>
    /// <param name="sharedPlanId">Identyfikator udostępnienia.</param>
    /// <response code="204">Udostępnienie zostało usunięte.</response>
    /// <response code="404">Udostępnienie nie istnieje lub nie należy do użytkownika.</response>
    [HttpDelete("shared/{sharedPlanId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unshare(Guid sharedPlanId, CancellationToken ct)
    {
        var ok = await _svc.UnshareAsync(sharedPlanId, ct);
        return ok ? NoContent() : NotFound();
    }
}

/// <summary>
/// Żądanie odpowiedzi na udostępniony plan.
/// </summary>
public sealed class RespondSharedPlanRequest
{
    /// <summary>
    /// Czy zaakceptować udostępniony plan.
    /// </summary>
    public bool Accept { get; set; }
}