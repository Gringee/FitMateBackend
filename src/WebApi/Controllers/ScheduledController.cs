using Application.Abstractions;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/scheduled")]
public class ScheduledController(IScheduledService svc) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScheduledDto dto, CancellationToken ct)
        => Ok(await svc.CreateAsync(dto, ct));

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await svc.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await svc.GetByIdAsync(id, ct)) is { } res ? Ok(res) : NotFound();

    [HttpGet("by-date")]
    public async Task<IActionResult> GetByDate([FromQuery] string date, CancellationToken ct)
        => Ok(await svc.GetByDateAsync(date, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateScheduledDto dto, CancellationToken ct)
        => (await svc.UpdateAsync(id, dto, ct)) is { } res ? Ok(res) : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => await svc.DeleteAsync(id, ct) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken ct)
        => (await svc.DuplicateAsync(id, ct)) is { } res ? Ok(res) : NotFound();
}
