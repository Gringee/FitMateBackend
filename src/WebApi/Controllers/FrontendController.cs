using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebApi.Controllers;

[ApiController]
[Route("api/frontend")]
public class FrontendController : ControllerBase
{
    private readonly IWorkoutService _svc;
    public FrontendController(IWorkoutService svc) => _svc = svc;

    /// <summary>Plan w formacie wymaganym przez SPA.</summary>
    [HttpGet("plan/{id:guid}")]
    public async Task<IActionResult> GetPlan(Guid id)
    {
        var dto = await _svc.GetPlanFrontendAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Zapisuje (POST) lub aktualizuje (PUT) zaplanowany trening z frontu.</summary>
    [HttpPost("scheduled")]
    public async Task<IActionResult> SaveScheduled([FromBody] FeScheduledWorkoutDto dto)
    {
        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Guid.Empty.ToString());

        var saved = await _svc.SaveScheduledFrontendAsync(dto, userId);

        return CreatedAtAction(
            nameof(GetPlan),
            new { id = saved.Id },
            saved);
    }
}
