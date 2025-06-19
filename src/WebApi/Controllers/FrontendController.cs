using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
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
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Guid.Empty.ToString());

        try
        {
            var saved = await _svc.SaveScheduledFrontendAsync(dto, userId);

            if (saved is null)
                return StatusCode(StatusCodes.Status500InternalServerError);

            return CreatedAtAction(
                nameof(GetPlan),
                new { id = saved.Id },
                saved);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = ex.Message });
        }
    }

    /// <summary>Wszystkie plany użytkownika na wskazany dzień (parametr „date” w formacie YYYY-MM-DD).</summary>
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlansForDay([FromQuery] string date)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var day))
            return BadRequest("date must be in YYYY-MM-DD format");

        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Guid.Empty.ToString());

        var list = await _svc.GetPlansByDateAsync(userId, day);
        return Ok(list);
    }

    /// <summary>Wszystkie dni (z id-kami), w których użytkownik ma trening.</summary>
    [HttpGet("days")]
    public async Task<IActionResult> GetWorkoutDays()
    {
        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Guid.Empty.ToString());

        var list = await _svc.GetAllWorkoutDaysAsync(userId);
        return Ok(list);
    }
}
