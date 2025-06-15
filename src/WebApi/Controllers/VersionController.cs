using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    [HttpGet]           // GET /api/version
    public ActionResult<VersionDto> Get()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "unknown";
        var buildStamp = System.IO.File.GetLastWriteTimeUtc(assembly.Location);

        return Ok(new VersionDto(version, buildStamp));
    }
}
