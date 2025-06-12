using Application.DTOs;
using Application.Services;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Application.Intertfaces;

namespace WebApi.Controllers;

/// <summary>
/// Operacje związane z planami treningowymi (Workout).
/// </summary>
[ApiController]
[Route("api/workouts")]
// Zostawiamy [Authorize], jeśli chcemy uwierzytelniać – do testów można pominąć.
// [Authorize]
public class WorkoutController : ControllerBase
{
    private readonly IWorkoutService _service;

    public WorkoutController(IWorkoutService service)
    {
        _service = service;
    }

    /// <summary>
    /// Tworzy nowy plan treningowy dla zalogowanego użytkownika.
    /// </summary>
    /// <param name="dto">Dane nowego planu (data, czas trwania, notatki oraz lista ćwiczeń).</param>
    /// <returns>200 OK, jeśli plan został dodany.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    public async Task<IActionResult> CreateWorkout([FromBody] CreateWorkoutDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

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

    /// <summary>
    /// Pobiera wszystkie plany treningowe zalogowanego użytkownika.
    /// </summary>
    /// <returns>Lista planów w formacie <see cref="Workout"/>.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IActionResult> GetWorkouts()
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        var workouts = await _service.GetAllForUserAsync(userId);
        return Ok(workouts);
    }

    /// <summary>
    /// Usuwa plan treningowy o podanym identyfikatorze (jeśli należy do zalogowanego użytkownika).
    /// </summary>
    /// <param name="id">Id planu treningowego do usunięcia.</param>
    /// <returns>204 No Content, jeśli usunięcie powiodło się.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> DeleteWorkout(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        await _service.DeleteWorkoutAsync(userId, id);
        return NoContent();
    }

    /// <summary>
    /// Aktualizuje istniejący plan treningowy (jeśli należy do zalogowanego użytkownika).
    /// </summary>
    /// <param name="id">Id planu do zaktualizowania.</param>
    /// <param name="dto">Nowe dane planu (data, czas trwania, notatki, lista ćwiczeń).</param>
    /// <returns>200 OK, jeśli aktualizacja się powiodła.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateWorkout(Guid id, [FromBody] CreateWorkoutDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        await _service.UpdateWorkoutAsync(userId, id, dto);
        return Ok();
    }
}
