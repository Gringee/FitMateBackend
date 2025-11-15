using Application.Abstractions;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Controllers;

[ApiController]
[Route("api/scheduled")]
[Authorize]
public class ScheduledController(IScheduledService svc) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ScheduledDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ScheduledDto>> Create([FromBody] CreateScheduledDto dto, CancellationToken ct)
    {
        var res = await svc.CreateAsync(dto, ct);
        return CreatedAtRoute("GetScheduledById", new { id = res.Id }, res);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ScheduledDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ScheduledDto>>> GetAll(CancellationToken ct)
        => Ok(await svc.GetAllAsync(ct));

    [HttpGet("{id:guid}", Name = "GetScheduledById")]
    [ProducesResponseType(typeof(ScheduledDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduledDto>> GetById(Guid id, CancellationToken ct)
        => (await svc.GetByIdAsync(id, ct)) is { } res ? Ok(res) : NotFound();
    
    [HttpGet("by-date")]
    [ProducesResponseType(typeof(List<ScheduledDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ScheduledDto>>> GetByDate([FromQuery] string date, CancellationToken ct)
        => Ok(await svc.GetByDateAsync(date, ct));

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ScheduledDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduledDto>> Update(Guid id, [FromBody] CreateScheduledDto dto, CancellationToken ct)
        => (await svc.UpdateAsync(id, dto, ct)) is { } res ? Ok(res) : NotFound();

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => await svc.DeleteAsync(id, ct) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/duplicate")]
    [ProducesResponseType(typeof(ScheduledDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduledDto>> Duplicate(Guid id, CancellationToken ct)
        => (await svc.DuplicateAsync(id, ct)) is { } res ? Ok(res) : NotFound();
}