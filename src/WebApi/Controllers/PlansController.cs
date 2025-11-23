using Application.Abstractions;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace WebApi.Controllers;

/// <summary>
/// Zarządzanie planami treningowymi oraz ich udostępnianiem.
/// </summary>
[Authorize]
[ApiController]
[Route("api/plans")]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class PlansController : ControllerBase
{
    private readonly IPlanService _svc;

    public PlansController(IPlanService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// Tworzy nowy plan treningowy.
    /// </summary>
    /// <remarks>
    /// Tworzy plan z listą ćwiczeń i serii, przypisując go do aktualnie zalogowanego użytkownika.
    /// 
    /// **Ograniczenia walidacji:**
    /// * `PlanName`: 3-100 znaków.
    /// * `Type`: 2-50 znaków (np. "Split", "FBW").
    /// * `Exercises`: Max 100 ćwiczeń w planie.
    /// * `Sets`: Max 50 serii na ćwiczenie.
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     POST /api/plans
    ///     {
    ///       "planName": "Mój Plan Siłowy",
    ///       "type": "Push Pull",
    ///       "notes": "Trening 4x w tygodniu",
    ///       "exercises": [
    ///         {
    ///           "name": "Wyciskanie sztangi",
    ///           "rest": 120,
    ///           "sets": [
    ///             { "reps": 8, "weight": 80 },
    ///             { "reps": 8, "weight": 80 }
    ///           ]
    ///         }
    ///       ]
    ///     }
    /// </remarks>
    /// <param name="dto">Kompletny obiekt planu wraz z ćwiczeniami.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Utworzony plan (wraz z nadanym ID).</returns>
    /// <response code="201">Plan został pomyślnie utworzony.</response>
    /// <response code="400">Błąd walidacji danych wejściowych (np. pusta nazwa, brak ćwiczeń).</response>
    [HttpPost]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlanDto>> Create(
        [FromBody] CreatePlanDto dto,
        CancellationToken ct)
    {
        var result = await _svc.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Pobiera listę planów użytkownika.
    /// </summary>
    /// <remarks>
    /// Zwraca plany stworzone przez zalogowanego użytkownika. Opcjonalnie może zwrócić również plany,
    /// które inni użytkownicy udostępnili Tobie (i które zaakceptowałeś).
    /// </remarks>
    /// <param name="includeShared">
    /// Jeśli <c>true</c>, lista zawiera również plany udostępnione (status <c>Accepted</c>). 
    /// Domyślnie <c>false</c> (tylko własne).
    /// </param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Lista planów.</returns>
    /// <response code="200">Lista planów.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlanDto>>> GetAll(
        [FromQuery] bool includeShared = false,
        CancellationToken ct = default)
    {
        var result = await _svc.GetAllAsync(includeShared, ct);
        return Ok(result);
    }

    /// <summary>
    /// Pobiera szczegóły konkretnego planu.
    /// </summary>
    /// <remarks>
    /// Zwraca pełną strukturę planu (ćwiczenia, serie).
    /// Dostępne tylko dla właściciela planu.
    /// </remarks>
    /// <param name="id">Identyfikator planu (GUID).</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="200">Szczegóły planu.</response>
    /// <response code="404">Plan nie istnieje lub nie masz do niego dostępu.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var res = await _svc.GetByIdAsync(id, ct);
        return res is not null ? Ok(res) : NotFound();
    }

    /// <summary>
    /// Aktualizuje istniejący plan.
    /// </summary>
    /// <remarks>
    /// Nadpisuje dane planu (nazwę, notatki, typ) oraz **wymienia całą listę ćwiczeń** na nową.
    /// Stare ćwiczenia i serie w tym planie są usuwane i zastępowane tymi z żądania.
    /// </remarks>
    /// <param name="id">Identyfikator edytowanego planu.</param>
    /// <param name="dto">Nowa definicja planu.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="200">Plan zaktualizowany pomyślnie.</response>
    /// <response code="400">Błąd walidacji.</response>
    /// <response code="404">Plan nie istnieje lub nie jest Twój.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    /// Trwale usuwa plan wraz ze wszystkimi ćwiczeniami i seriami.
    /// </remarks>
    /// <param name="id">Identyfikator planu.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="204">Plan usunięty.</response>
    /// <response code="404">Plan nie istnieje lub nie jest Twój.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>
    /// Duplikuje (kopiuje) istniejący plan.
    /// </summary>
    /// <remarks>
    /// Tworzy głęboką kopię planu (wraz z ćwiczeniami i seriami), dodając do nazwy dopisek "(Copy)".
    /// Nowy plan staje się własnością użytkownika wykonującego akcję.
    /// </remarks>
    /// <param name="id">Identyfikator planu źródłowego.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Nowy, skopiowany obiekt planu.</returns>
    /// <response code="201">Kopia planu została utworzona.</response>
    /// <response code="404">Plan źródłowy nie istnieje.</response>
    [HttpPost("{id:guid}/duplicate")]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken ct)
    {
        var res = await _svc.DuplicateAsync(id, ct);
        if (res is null) return NotFound();
        
        return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
    }

    // --- SEKCJA SHARING (UDOSTĘPNIANIE) ---

    /// <summary>
    /// Udostępnia plan innemu użytkownikowi.
    /// </summary>
    /// <remarks>
    /// Wysyła zaproszenie do współdzielenia planu. Odbiorca zobaczy je w sekcji "Pending" i będzie mógł je zaakceptować.
    /// 
    /// **Ograniczenia:**
    /// * Nie możesz udostępnić planu samemu sobie.
    /// * Nie możesz udostępnić planu, który nie należy do Ciebie.
    /// * Jeśli plan jest już udostępniony tej osobie (nawet w statusie Pending), operacja zwróci błąd.
    /// </remarks>
    /// <param name="planId">Identyfikator Twojego planu.</param>
    /// <param name="targetUserId">Identyfikator użytkownika (GUID), któremu chcesz pokazać plan.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="200">Zaproszenie do udostępnienia zostało wysłane.</response>
    /// <response code="400">Błąd biznesowy (np. już udostępniono, target == user).</response>
    /// <response code="404">Nie znaleziono planu lub użytkownika docelowego.</response>
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
    /// Pobiera listę planów udostępnionych MI przez innych (status Accepted).
    /// </summary>
    /// <remarks>
    /// Są to "cudze" plany, do których użytkownik uzyskał dostęp i je zaakceptował.
    /// </remarks>
    /// <response code="200">Lista planów.</response>
    [HttpGet("shared-with-me")]
    [ProducesResponseType(typeof(IReadOnlyList<PlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlanDto>>> GetSharedWithMe(CancellationToken ct)
    {
        var result = await _svc.GetSharedWithMeAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Pobiera otrzymane zaproszenia do planów (Oczekujące).
    /// </summary>
    /// <remarks>
    /// Lista udostępnień skierowanych do Ciebie, które mają status <c>Pending</c> (czekają na decyzję).
    /// </remarks>
    /// <response code="200">Lista oczekujących udostępnień (Przychodzące).</response>
    [HttpGet("shared/pending")]
    [ProducesResponseType(typeof(IReadOnlyList<SharedPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SharedPlanDto>>> GetPendingSharedPlans(CancellationToken ct)
    {
        var items = await _svc.GetPendingSharedPlansAsync(ct);
        return Ok(items);
    }
    
    /// <summary>
    /// Pobiera wysłane zaproszenia do planów (Oczekujące).
    /// </summary>
    /// <remarks>
    /// Lista udostępnień, które Ty wysłałeś innym, ale oni jeszcze nie podjęli decyzji (status <c>Pending</c>).
    /// </remarks>
    /// <response code="200">Lista oczekujących udostępnień (Wychodzące).</response>
    [HttpGet("shared/sent/pending")]
    [ProducesResponseType(typeof(IReadOnlyList<SharedPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SharedPlanDto>>> GetSentPendingSharedPlans(CancellationToken ct)
    {
        var items = await _svc.GetSentPendingSharedPlansAsync(ct);
        return Ok(items);
    }

    /// <summary>
    /// Decyzja o przyjęciu lub odrzuceniu udostępnionego planu.
    /// </summary>
    /// <remarks>
    /// Pozwala zmienić status zaproszenia z <c>Pending</c> na <c>Accepted</c> lub <c>Rejected</c>.
    /// 
    /// **Przykład ciała żądania:**
    /// 
    ///     { "accept": true }
    ///     
    /// </remarks>
    /// <param name="sharedPlanId">Identyfikator wpisu udostępnienia (SharedPlan ID).</param>
    /// <param name="body">Obiekt decyzyjny.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="200">Status zaktualizowany.</response>
    /// <response code="400">Zaproszenie nie jest już w statusie Pending.</response>
    /// <response code="404">Nie znaleziono zaproszenia.</response>
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
    /// Historia udostępnień (Zakończone).
    /// </summary>
    /// <remarks>
    /// Zwraca historię udostępnień, które zostały już rozpatrzone (status <c>Accepted</c> lub <c>Rejected</c>).
    /// Plany w statusie <c>Pending</c> są pomijane (do tego służą osobne endpointy).
    /// 
    /// **Parametr scope:**
    /// * `received` (domyślny) – Plany otrzymane od innych.
    /// * `sent` – Plany wysłane przez Ciebie innym.
    /// * `all` – Wszystkie powiązane z Tobą.
    /// </remarks>
    /// <param name="scope">Zakres historii: <c>received</c>, <c>sent</c>, <c>all</c>.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Lista historycznych wpisów udostępnień.</returns>
    [HttpGet("shared/history")]
    [ProducesResponseType(typeof(IReadOnlyList<SharedPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SharedPlanDto>>> GetSharedHistory(
        [FromQuery] string? scope,
        CancellationToken ct)
    {
        var items = await _svc.GetSharedHistoryAsync(scope, ct);
        return Ok(items);
    }
    
    /// <summary>
    /// Usuwa lub anuluje udostępnienie planu.
    /// </summary>
    /// <remarks>
    /// Pozwala wycofać udostępnienie (jeśli jesteś nadawcą) lub usunąć plan z listy "Udostępnione mi" (jeśli jesteś odbiorcą).
    /// 
    /// **Tryb onlyPending:**
    /// * Jeśli <c>true</c>: Usuwa wpis TYLKO jeśli jest w statusie <c>Pending</c> (anulowanie zaproszenia).
    /// * Jeśli <c>false</c> (domyślnie): Usuwa wpis bez względu na status (np. odebranie dostępu po czasie).
    /// </remarks>
    /// <param name="sharedPlanId">Identyfikator wpisu udostępnienia.</param>
    /// <param name="onlyPending">Czy ograniczyć usuwanie tylko do oczekujących zaproszeń.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="204">Udostępnienie usunięte.</response>
    /// <response code="400">Błąd logiczny (np. wymuszono onlyPending, a status był Accepted).</response>
    /// <response code="404">Nie znaleziono wpisu udostępnienia.</response>
    [HttpDelete("shared/{sharedPlanId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSharedPlan(Guid sharedPlanId, [FromQuery] bool onlyPending = false, CancellationToken ct = default)
    {
        await _svc.DeleteSharedPlanAsync(sharedPlanId, onlyPending, ct);
        return NoContent();
    }
}