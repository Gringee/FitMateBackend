using Application.Abstractions;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace WebApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
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
    /// Pobiera listę wszystkich użytkowników.
    /// </summary>
    /// <remarks>
    /// Pozwala na filtrowanie wyników. Wyszukiwanie jest "case-insensitive" (niewrażliwe na wielkość liter).
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     GET /api/users?search=kowalski
    ///     
    /// </remarks>
    /// <param name="search">
    /// (Opcjonalne) Fraza filtrująca. Przeszukuje pola: <c>FullName</c>, <c>Email</c> oraz <c>UserName</c>.
    /// </param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Lista obiektów DTO użytkowników.</returns>
    /// <response code="200">Zwraca listę użytkowników (pustą, jeśli nie znaleziono pasujących).</response>
    /// <response code="401">Brak autoryzacji lub niewystarczające uprawnienia (wymagana rola Admin).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll([FromQuery] string? search, CancellationToken ct)
    {
        var users = await _userService.GetAllAsync(search, ct);
        return Ok(users);
    }

    /// <summary>
    /// Tworzy nowego użytkownika (ręcznie przez Admina).
    /// </summary>
    /// <remarks>
    /// Umożliwia administratorowi dodanie użytkownika z pominięciem procesu rejestracji publicznej.
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     POST /api/users
    ///     {
    ///        "fullName": "Nowy Pracownik",
    ///        "email": "pracownik@firma.com",
    ///        "userName": "nowy_pracownik"
    ///     }
    ///     
    /// **Uwaga:** Hasło nie jest ustawiane w tym kroku (lub jest puste/domyślne, zależnie od logiki encji), 
    /// administrator powinien użyć endpointu resetowania hasła lub użytkownik powinien skorzystać z procesu "Zapomniałem hasła".
    /// </remarks>
    /// <param name="dto">Dane nowego użytkownika.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Utworzony użytkownik.</returns>
    /// <response code="201">Użytkownik został utworzony pomyślnie.</response>
    /// <response code="400">
    /// Błąd walidacji danych lub konflikt biznesowy (np. zajęty email/login).
    /// Szczegóły błędu znajdują się w ciele odpowiedzi (ProblemDetails).
    /// </response>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        // UserService rzuca wyjątki (ArgumentException, InvalidOperationException), 
        // które Twój ExceptionHandlingMiddleware zamieni na 400 Bad Request.
        var result = await _userService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    /// <summary>
    /// Aktualizuje dane istniejącego użytkownika.
    /// </summary>
    /// <remarks>
    /// Pozwala zmienić nazwę wyświetlaną, email lub nazwę użytkownika.
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     PUT /api/users/3fa85f64-5717-4562-b3fc-2c963f66afa6
    ///     {
    ///        "fullName": "Zaktualizowany Jan",
    ///        "email": "nowy.email@firma.com",
    ///        "userName": "jan.nowy"
    ///     }
    ///     
    /// </remarks>
    /// <param name="id">Identyfikator użytkownika (GUID).</param>
    /// <param name="dto">Zmienione dane.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="204">Dane zostały zaktualizowane.</response>
    /// <response code="400">Błąd walidacji lub konflikt (np. nowy email jest już zajęty).</response>
    /// <response code="404">Użytkownik o podanym ID nie istnieje.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto, CancellationToken ct)
    {
        await _userService.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    /// <summary>
    /// Przypisuje rolę systemową do użytkownika.
    /// </summary>
    /// <remarks>
    /// Służy do nadawania uprawnień (np. awans na Admina).
    /// 
    /// **Przykładowe użycie:**
    /// 
    ///     PUT /api/users/{id}/role?roleName=Admin
    ///     
    /// </remarks>
    /// <param name="id">Identyfikator użytkownika.</param>
    /// <param name="roleName">Nazwa roli (np. <c>Admin</c>, <c>User</c>). Wielkość liter ma znaczenie.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="204">Rola została pomyślnie przypisana.</response>
    /// <response code="400">Podano nieprawidłową nazwę roli (lub rola nie istnieje w bazie).</response>
    /// <response code="404">Nie znaleziono użytkownika o podanym ID.</response>
    /// <response code="409">Konflikt – użytkownik posiada już przypisaną tę rolę.</response>
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
    /// Usuwa użytkownika z systemu.
    /// </summary>
    /// <remarks>
    /// **Ważne:** Nie można usunąć użytkownika posiadającego rolę <c>Admin</c> ze względów bezpieczeństwa.
    /// </remarks>
    /// <param name="id">Identyfikator użytkownika do usunięcia.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="204">Użytkownik został usunięty.</response>
    /// <response code="400">Próba usunięcia administratora (operacja zabroniona).</response>
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
    /// Resetuje hasło użytkownika (wymuszenie zmiany).
    /// </summary>
    /// <remarks>
    /// Administrator ustawia nowe hasło dla wskazanego użytkownika. Stare hasło zostaje nadpisane nowym hashem.
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     POST /api/users/{id}/reset-password
    ///     {
    ///        "newPassword": "SuperTajneHaslo123!"
    ///     }
    ///     
    /// </remarks>
    /// <param name="id">Identyfikator użytkownika.</param>
    /// <param name="dto">Obiekt zawierający nowe hasło (minimum 8 znaków).</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="204">Hasło zostało pomyślnie zmienione.</response>
    /// <response code="400">Hasło nie spełnia wymagań walidacji (np. za krótkie).</response>
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