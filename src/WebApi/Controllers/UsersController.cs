using Application.Abstractions;
using Application.DTOs;
using Domain.Entities;
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
    private readonly IUserService _userService;
    private readonly AppDbContext _db;

    public UsersController(IUserService userService, AppDbContext db)
    {
        _userService = userService;
        _db = db;
    }

    // GET /api/users?search=filip
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll([FromQuery] string? search, CancellationToken ct)
    {
        var users = await _userService.GetAllAsync(search, ct);
        return Ok(users);
    }

    // PUT /api/users/{id}/role?roleName=Admin
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> AssignRole(Guid id, [FromQuery] string roleName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("Role name is required.");

        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
            return NotFound();

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
        if (role is null)
            return BadRequest("Role not found.");

        if (!user.UserRoles.Any(ur => ur.RoleId == role.Id))
        {
            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            await _db.SaveChangesAsync(ct);
        }

        return NoContent();
    }
}