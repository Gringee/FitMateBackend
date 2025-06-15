using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BodyPartsController : ControllerBase
{
    private readonly IBodyPartService _svc;
    public BodyPartsController(IBodyPartService svc) => _svc = svc;

    /// <summary>Lista wszystkich partii ciała.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BodyPartDto>>> GetAll()
        => Ok(await _svc.GetAllAsync());

    /// <summary>Pojedyncza partia ciała.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BodyPartDto>> Get(Guid id)
        => (await _svc.GetByIdAsync(id)) is { } dto ? Ok(dto) : NotFound();

    /// <summary>Tworzy nową partię ciała.</summary>
    [HttpPost]
    public async Task<ActionResult<BodyPartDto>> Create(CreateBodyPartDto dto)
    {
        var created = await _svc.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.BodyPartId }, created);
    }

    /// <summary>Aktualizuje istniejącą partię ciała.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateBodyPartDto dto)
        => await _svc.UpdateAsync(id, dto) ? NoContent() : NotFound();

    /// <summary>Usuwa partię ciała.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await _svc.DeleteAsync(id) ? NoContent() : NotFound();
}
