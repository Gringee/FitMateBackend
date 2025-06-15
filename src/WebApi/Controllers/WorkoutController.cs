using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebApi.Controllers;

/// <summary>
/// Operacje związane z planami treningowymi (Workout).
/// </summary>
[ApiController]
[Route("api/workouts")]
public class WorkoutController : ControllerBase
{
    private readonly IWorkoutService _service;

    public WorkoutController(IWorkoutService service) => _service = service;

    /// <summary>Tworzy nowy plan treningowy dla zalogowanego użytkownika.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWorkout([FromBody] CreateWorkoutDto dto)
    {
        var userId = GetUserId();
        try
        {
            await _service.CreateWorkoutAsync(userId, dto);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Pobiera wszystkie plany treningowe zalogowanego użytkownika.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkouts()
    {
        var userId = GetUserId();
        var workouts = await _service.GetAllForUserAsync(userId);
        return Ok(workouts);
    }

    /// <summary>Zwraca pojedynczy plan treningowy.</summary>
    /// <param name="id">Id planu.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        var all = await _service.GetAllForUserAsync(userId);
        var single = all.FirstOrDefault(w => w.WorkoutId == id);
        return single is null ? NotFound() : Ok(single);
    }

    /// <summary>Aktualizuje istniejący plan treningowy.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWorkout(Guid id, [FromBody] CreateWorkoutDto dto)
    {
        var userId = GetUserId();
        await _service.UpdateWorkoutAsync(userId, id, dto);
        return Ok();
    }

    /// <summary>Usuwa plan treningowy (jeśli należy do użytkownika).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWorkout(Guid id)
    {
        var userId = GetUserId();
        await _service.DeleteWorkoutAsync(userId, id);
        return NoContent();
    }

    /// <summary>Duplikuje istniejący plan treningowy.</summary>
    [HttpPost("{id:guid}/duplicate")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Duplicate(Guid id, DuplicateWorkoutDto dto)
    {
        var userId = GetUserId();
        var result = await _service.DuplicateAsync(userId, id, dto);

        return result is null
            ? NotFound()
            : CreatedAtAction(nameof(GetById), new { id = result.WorkoutId }, result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? Guid.Empty.ToString());
}
