using Application.DTOs;
using Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly IWorkoutSessionService _svc;
    public SessionsController(IWorkoutSessionService svc) => _svc = svc;

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartSessionRequest request, CancellationToken ct)
        => Ok(await _svc.StartAsync(request, ct));

    [HttpPatch("{id:guid}/set")]
    public async Task<IActionResult> PatchSet(Guid id, [FromBody] PatchSetRequest req, CancellationToken ct)
        => Ok(await _svc.PatchSetAsync(id, req, ct));

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteSessionRequest req, CancellationToken ct)
        => Ok(await _svc.CompleteAsync(id, req, ct));

    [HttpPost("{id:guid}/abort")]
    public async Task<IActionResult> Abort(Guid id, [FromBody] AbortSessionRequest req, CancellationToken ct)
        => Ok(await _svc.AbortAsync(id, req, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _svc.GetByIdAsync(id, ct)) is { } dto ? Ok(dto) : NotFound();

    [HttpGet("by-range")]
    public async Task<IActionResult> ByRange([FromQuery] string from, [FromQuery] string to, CancellationToken ct)
    {
        var fromUtc = DateTime.SpecifyKind(DateTime.Parse(from), DateTimeKind.Utc);
        var toUtc   = DateTime.SpecifyKind(DateTime.Parse(to), DateTimeKind.Utc);
        return Ok(await _svc.GetByRangeAsync(fromUtc, toUtc, ct));
    }
}