using Application.DTOs;
using Application.Services;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Controllers
{
    /// <summary>
    /// Zarządzanie słownikiem ćwiczeń (Exercise).
    /// </summary>
    [ApiController]
    [Route("api/exercises")]
    public class ExerciseController : ControllerBase
    {
        private readonly IExerciseService _service;

        public ExerciseController(IExerciseService service)
        {
            _service = service;
        }

        /// <summary>
        /// Tworzy nowe ćwiczenie.
        /// </summary>
        /// <param name="dto">Dane nowego ćwiczenia.</param>
        /// <returns>Utworzone ExerciseDto wraz z HTTP 201 Created.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody, Required] CreateExerciseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.ExerciseId }, created);
        }

        /// <summary>
        /// Pobiera wszystkie ćwiczenia.
        /// </summary>
        /// <returns>Lista ExerciseDto.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        /// <summary>
        /// Zwraca pojedyncze ćwiczenie na podstawie ID.
        /// </summary>
        /// <param name="id">Identyfikator ćwiczenia.</param>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var e = await _service.GetByIdAsync(id);
            if (e == null)
                return NotFound(new { error = $"Ćwiczenie o ID {id} nie istnieje." });

            return Ok(e);
        }

        /// <summary>
        /// Aktualizuje istniejące ćwiczenie.
        /// </summary>
        /// <param name="id">Identyfikator ćwiczenia do aktualizacji.</param>
        /// <param name="dto">Nowe dane ćwiczenia.</param>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExerciseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool updated = await _service.UpdateAsync(id, dto);
            if (!updated)
                return NotFound(new { error = $"Ćwiczenie o ID {id} nie istnieje." });

            return Ok();
        }

        /// <summary>
        /// Usuwa dane ćwiczenie.
        /// </summary>
        /// <param name="id">Identyfikator ćwiczenia do usunięcia.</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { error = $"Ćwiczenie o ID {id} nie istnieje." });

            return NoContent();
        }
    }
}
