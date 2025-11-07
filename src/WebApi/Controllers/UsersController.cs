using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _db.Users
            .Select(u => new { u.Id, u.Email, u.FullName })
            .ToListAsync(ct);
        return Ok(users);
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> AssignRole(Guid id, [FromQuery] string roleName, CancellationToken ct)
    {
        var user = await _db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound();

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
        if (role is null) return BadRequest("Role not found.");

        if (!user.UserRoles.Any(ur => ur.RoleId == role.Id))
        {
            _db.UserRoles.Add(new Domain.Entities.UserRole { UserId = user.Id, RoleId = role.Id });
            await _db.SaveChangesAsync(ct);
        }
        return NoContent();
    }
}