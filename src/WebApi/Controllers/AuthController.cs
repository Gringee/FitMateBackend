using Application.Abstractions;
using Application.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace WebApi.Controllers;

[ApiController]
[Route("api/auth")]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Rejestracja nowego konta użytkownika.
    /// </summary>
    /// <remarks>
    /// Tworzy nowego użytkownika w systemie, przypisuje mu domyślną rolę i automatycznie loguje, zwracając tokeny.
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     POST /api/auth/register
    ///     {
    ///        "email": "jan.kowalski@example.com",
    ///        "username": "jankowal",
    ///        "password": "TrudneHaslo123!",
    ///        "fullName": "Jan Kowalski"
    ///     }
    /// 
    /// **Logika biznesowa:**
    /// * Email i UserName muszą być unikalne.
    /// * Hasło jest hashowane przed zapisem (BCrypt).
    /// </remarks>
    /// <param name="req">Obiekt DTO zawierający wymagane dane rejestracyjne.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Zwraca parę tokenów (Access + Refresh).</returns>
    /// <response code="200">Rejestracja przebiegła pomyślnie, zwrócono tokeny.</response>
    /// <response code="400">
    /// Błąd walidacji lub danych biznesowych.
    /// Możliwe przyczyny (zwracane w polu `detail` struktury ProblemDetails):
    /// * "Email already registered."
    /// * "Username already registered."
    /// </response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest req,
        CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(req, ct);
        return Ok(result);
    }

    /// <summary>
    /// Logowanie użytkownika.
    /// </summary>
    /// <remarks>
    /// Weryfikuje poświadczenia i generuje nowe tokeny dostępowe.
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     POST /api/auth/login
    ///     {
    ///        "userNameOrEmail": "jankowal", 
    ///        "password": "TrudneHaslo123!"
    ///     }
    /// 
    /// Uwaga: Pole `userNameOrEmail` akceptuje zarówno login jak i adres e-mail.
    /// </remarks>
    /// <param name="req">Dane logowania (Login/Email + Hasło).</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Zwraca parę tokenów (Access + Refresh).</returns>
    /// <response code="200">Logowanie poprawne.</response>
    /// <response code="400">
    /// Błędne dane logowania.
    /// Zwracany komunikat: "Invalid credentials."
    /// </response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest req,
        CancellationToken ct)
    {
        var result = await _auth.LoginAsync(req, ct);
        return Ok(result);
    }

    /// <summary>
    /// Odświeżanie tokena dostępowego (Refresh Token Flow).
    /// </summary>
    /// <remarks>
    /// Pozwala uzyskać nowy `accessToken` bez konieczności ponownego logowania, używając ważnego `refreshToken`.
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     POST /api/auth/refresh
    ///     {
    ///        "refreshToken": "8a7b...123z"
    ///     }
    /// 
    /// </remarks>
    /// <param name="request">Obiekt DTO zawierający token odświeżania.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Nowa para tokenów (Access + ten sam lub nowy Refresh w zależności od implementacji).</returns>
    /// <response code="200">Token został pomyślnie odświeżony.</response>
    /// <response code="400">
    /// Przesłany refresh token jest nieprawidłowy, wygasł lub został unieważniony.
    /// Komunikat: "Invalid refresh token."
    /// </response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshRequestDto request,
        CancellationToken ct)
    {
        var result = await _auth.RefreshAsync(request.RefreshToken, ct);
        return Ok(result);
    }

    /// <summary>
    /// Wylogowanie (Unieważnienie Refresh Tokena).
    /// </summary>
    /// <remarks>
    /// Usuwa wskazany `refreshToken` z bazy danych, zapobiegając jego dalszemu użyciu do generowania nowych tokenów dostępowych.
    /// Operacja jest idempotentna – jeśli token nie istnieje, endpoint również zwraca 204.
    /// 
    /// **Wymagania:**
    /// * Użytkownik musi być zalogowany (wymagany nagłówek `Authorization: Bearer ...`).
    /// </remarks>
    /// <param name="dto">Obiekt zawierający token do unieważnienia.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="204">Pomyślnie wylogowano (token usunięty lub nie istniał).</response>
    /// <response code="401">Brak ważnego tokena dostępowego w nagłówku (użytkownik niezalogowany).</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequestDto dto,
        CancellationToken ct)
    {
        await _auth.LogoutAsync(dto, ct);
        return NoContent();
    }
}