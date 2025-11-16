using Application.Abstractions;
using Application.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Rejestruje nowego użytkownika.
    /// </summary>
    /// <remarks>
    /// Tworzy użytkownika, przypisuje mu rolę <c>User</c> i od razu zwraca parę
    /// <c>accessToken</c> + <c>refreshToken</c>.
    /// </remarks>
    /// <param name="req">Dane rejestracyjne (email, hasło, pełne imię, nazwa użytkownika).</param>
    /// <response code="200">Pomyślna rejestracja – zwrócone tokeny.</response>
    /// <response code="400">
    /// Niepoprawne dane (np. zła walidacja pola albo email/username już zajęty).
    /// </response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest req,
        CancellationToken ct)
    {
        // Błędy ModelState → globalny InvalidModelStateResponseFactory (ProblemDetails 400)
        var result = await _auth.RegisterAsync(req, ct);
        return Ok(result);
    }

    /// <summary>
    /// Loguje użytkownika i zwraca tokeny.
    /// </summary>
    /// <remarks>
    /// Pole <c>UserNameOrEmail</c> może zawierać zarówno nazwę użytkownika, jak i adres e-mail.
    /// Zwracany jest nowy <c>accessToken</c> oraz <c>refreshToken</c>.
    /// </remarks>
    /// <param name="req">Login (UserName lub Email) oraz hasło.</param>
    /// <response code="200">Logowanie udane – zwrócone tokeny.</response>
    /// <response code="400">
    /// Niepoprawne dane wejściowe lub błędne dane logowania
    /// (mapowane z wyjątków w <c>AuthService</c> przez globalny middleware).
    /// </response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest req,
        CancellationToken ct)
    {
        var result = await _auth.LoginAsync(req, ct);
        return Ok(result);
    }

    /// <summary>
    /// Odświeża access token na podstawie refresh tokenu.
    /// </summary>
    /// <remarks>
    /// Przyjmuje ważny <c>refreshToken</c> i zwraca nowy <c>accessToken</c>.
    /// Aktualnie backend zwraca ten sam refresh token (można to później rozszerzyć
    /// o rotację refresh tokenów).
    /// </remarks>
    /// <param name="request">Obiekt zawierający refresh token.</param>
    /// <response code="200">Nowy access token wygenerowany.</response>
    /// <response code="400">
    /// Refresh token nieprawidłowy lub wygasły (np. <c>InvalidOperationException</c> z serwisu).
    /// </response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshRequestDto request,
        CancellationToken ct)
    {
        var result = await _auth.RefreshAsync(request.RefreshToken, ct);
        return Ok(result);
    }

    /// <summary>
    /// Wylogowuje użytkownika (unieważnia refresh token).
    /// </summary>
    /// <remarks>
    /// Usuwa wskazany <c>refreshToken</c> z bazy.
    /// Access token wygaśnie sam – backend go nie „zabija” natychmiast.
    /// </remarks>
    /// <param name="dto">Obiekt z refresh tokenem do unieważnienia.</param>
    /// <response code="204">
    /// Refresh token został unieważniony (lub nie istniał – operacja jest idempotentna).
    /// </response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequestDto dto,
        CancellationToken ct)
    {
        await _auth.LogoutAsync(dto, ct);
        return NoContent();
    }
}