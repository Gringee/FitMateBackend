using Application.Abstractions;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserAdminService _adminService;

    public UsersController(IUserService userService, IUserAdminService adminService)
    {
        _userService = userService;
        _adminService = adminService;
    }

    /// <summary>
    /// Zwraca listę użytkowników (tylko dla Admina).
    /// </summary>
    /// <remarks>
    /// Możesz filtrować po fragmencie imienia, adresu e-mail lub nazwy użytkownika.
    /// </remarks>
    /// <param name="search">
    /// (Opcjonalne) fragment <c>FullName</c>, <c>Email</c> albo <c>UserName</c>.
    /// </param>
    /// <response code="200">Lista użytkowników.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserDto>))]
    public async Task<ActionResult<List<UserDto>>> GetAll([FromQuery] string? search, CancellationToken ct)
    {
        var users = await _userService.GetAllAsync(search, ct);
        return Ok(users);
    }

    /// <summary>
    /// Przypisuje rolę do użytkownika.
    /// </summary>
    /// <remarks>
    /// Przykład: <c>roleName=Admin</c> albo <c>roleName=User</c>.<br/>
    /// Jeśli użytkownik już ma tę rolę, zwrócone zostanie 409 (Conflict).
    /// </remarks>
    /// <param name="id">Id użytkownika.</param>
    /// <param name="roleName">Nazwa roli (np. <c>Admin</c>, <c>User</c>).</param>
    /// <response code="204">Rola została przypisana.</response>
    /// <response code="400">Niepoprawna nazwa roli lub brak <c>roleName</c>.</response>
    /// <response code="404">Użytkownik lub rola nie istnieje.</response>
    /// <response code="409">Użytkownik już posiada tę rolę.</response>
    [HttpPut("{id:guid}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignRole(Guid id, [FromQuery] string roleName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest("Role name is required.");

        var result = await _adminService.AssignRoleAsync(id, roleName, ct);

        return result switch
        {
            AssignRoleResult.UserNotFound   => NotFound("User not found."),
            AssignRoleResult.RoleNotFound   => BadRequest("Role not found."),
            AssignRoleResult.AlreadyHasRole => Conflict("User already has this role."),
            AssignRoleResult.Ok             => NoContent(),
            _                               => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    /// <summary>
    /// Usuwa użytkownika (oprócz użytkowników z rolą Admin).
    /// </summary>
    /// <param name="id">Id użytkownika do usunięcia.</param>
    /// <response code="204">Użytkownik został usunięty.</response>
    /// <response code="400">Próba usunięcia użytkownika z rolą Admin.</response>
    /// <response code="404">Użytkownik nie istnieje.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _adminService.DeleteUserAsync(id, ct);

        return result switch
        {
            DeleteUserResult.NotFound => NotFound(),
            DeleteUserResult.IsAdmin  => BadRequest("Nie można usunąć użytkownika z rolą Admin."),
            DeleteUserResult.Ok       => NoContent(),
            _                         => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    /// <summary>
    /// Resetuje hasło użytkownika.
    /// </summary>
    /// <remarks>
    /// Hasło jest nadpisywane nową wartością i ponownie hashowane w bazie.
    /// Używaj tylko jako Admin (np. gdy użytkownik zapomni hasła).
    /// </remarks>
    /// <param name="id">Id użytkownika.</param>
    /// <param name="dto">Nowe hasło.</param>
    /// <response code="204">Hasło zostało zresetowane.</response>
    /// <response code="400">Niepoprawne dane wejściowe (za krótkie hasło itp.).</response>
    /// <response code="404">Użytkownik nie istnieje.</response>
    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await _adminService.ResetPasswordAsync(id, dto.NewPassword, ct);

        return result switch
        {
            ResetPasswordResult.NotFound => NotFound(),
            ResetPasswordResult.Ok       => NoContent(),
            _                            => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}