using Application.Abstractions;
using Application.DTOs.Analytics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _svc;
    public AnalyticsController(IAnalyticsService svc) => _svc = svc;

    [HttpGet("overview")]
    public async Task<ActionResult<OverviewDto>> Overview([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
        => Ok(await _svc.GetOverviewAsync(from, to, ct));

    [HttpGet("volume")]
    public async Task<ActionResult<IReadOnlyList<TimePointDto>>> Volume(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string groupBy = "day",
        [FromQuery] string? exerciseName = null,
        CancellationToken ct = default)
        => Ok(await _svc.GetVolumeAsync(from, to, groupBy, exerciseName, ct));

    [HttpGet("exercises/{name}/e1rm")]
    public async Task<ActionResult<IReadOnlyList<E1rmPointDto>>> E1rm(
        [FromRoute] string name,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
        => Ok(await _svc.GetE1RmAsync(name, from, to, ct));

    [HttpGet("adherence")]
    public async Task<ActionResult<AdherenceDto>> Adherence(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        CancellationToken ct)
        => Ok(await _svc.GetAdherenceAsync(fromDate, toDate, ct));

    [HttpGet("plan-vs-actual")]
    public async Task<ActionResult<IReadOnlyList<PlanVsActualItemDto>>> PlanVsActual(
        [FromQuery] Guid sessionId, CancellationToken ct)
        => Ok(await _svc.GetPlanVsActualAsync(sessionId, ct));
}