using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Abstractions;

namespace WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/plans")]
public class PlansController(IPlanService svc) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlanDto dto, CancellationToken ct)
        => Ok(await svc.CreateAsync(dto, ct));

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await svc.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await svc.GetByIdAsync(id, ct)) is { } res ? Ok(res) : NotFound();

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreatePlanDto dto, CancellationToken ct)
        => (await svc.UpdateAsync(id, dto, ct)) is { } res ? Ok(res) : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => await svc.DeleteAsync(id, ct) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken ct)
        => (await svc.DuplicateAsync(id, ct)) is { } res ? Ok(res) : NotFound();
    
    [HttpPost("{planId:guid}/share-to/{targetUserId:guid}")]
    public async Task<IActionResult> ShareToUser(Guid planId, Guid targetUserId, CancellationToken ct)
    {
        await svc.ShareToUserAsync(planId, targetUserId, ct);
        return Ok(new { message = "Plan udostępniony pomyślnie." });
    }
    
    [HttpGet("shared-with-me")]
    public async Task<ActionResult<List<PlanDto>>> GetSharedWithMe(CancellationToken ct)
    {
        var result = await svc.GetSharedWithMeAsync(ct);
        return Ok(result);
    }
    
    [HttpGet("shared/pending")]
    public async Task<ActionResult<List<SharedPlanDto>>> GetPendingSharedPlans(CancellationToken ct)
    {
        var items = await svc.GetPendingSharedPlansAsync(ct);
        return Ok(items);
    }
    
    [HttpPost("shared/{sharedPlanId:guid}/respond")]
    public async Task<IActionResult> RespondToSharedPlan(
        Guid sharedPlanId,
        [FromBody] RespondSharedPlanRequest body,
        CancellationToken ct)
    {
        await svc.RespondToSharedPlanAsync(sharedPlanId, body.Accept, ct);
        return Ok(new { message = body.Accept ? "Plan zaakceptowany." : "Plan odrzucony." });
    }
    
    [HttpGet("shared/history")]
    public async Task<ActionResult<List<SharedPlanDto>>> GetSharedHistory(CancellationToken ct)
    {
        var items = await svc.GetSharedHistoryAsync(ct);
        return Ok(items);
    }
    
    [HttpDelete("shared/{sharedPlanId:guid}")]
    public async Task<IActionResult> Unshare(Guid sharedPlanId, CancellationToken ct)
    {
        var ok = await svc.UnshareAsync(sharedPlanId, ct);
        return ok ? NoContent() : NotFound();
    }
}

public sealed class RespondSharedPlanRequest
{
    public bool Accept { get; set; }
}