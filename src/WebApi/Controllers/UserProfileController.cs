using Application.Abstractions;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace WebApi.Controllers;

/// <summary>
/// Operacje związane z profilem aktualnie zalogowanego użytkownika.
/// </summary>
[Authorize]
[ApiController]
[Route("api/userprofile")]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public sealed class UserProfileController : ControllerBase
{
    private readonly IUserProfileService _svc;

    public UserProfileController(IUserProfileService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// Pobiera dane profilowe zalogowanego użytkownika.
    /// </summary>
    /// <remarks>
    /// Zwraca podstawowe informacje o koncie (ID, login, email, imię i nazwisko) oraz listę przypisanych ról.
    /// Identyfikacja użytkownika odbywa się na podstawie tokena JWT (Claims).
    /// </remarks>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Obiekt DTO z danymi profilu.</returns>
    /// <response code="200">Zwraca dane profilowe.</response>
    /// <response code="401">Użytkownik niezalogowany (brak lub nieważny token).</response>
    /// <response code="404">Nie znaleziono użytkownika w bazie (np. konto usunięte, a token jeszcze ważny).</response>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetMe(CancellationToken ct)
    {
        var dto = await _svc.GetCurrentAsync(ct);
        return Ok(dto);
    }

    /// <summary>
    /// Aktualizuje dane profilowe (Imię, Email, Login).
    /// </summary>
    /// <remarks>
    /// Pozwala użytkownikowi na samodzielną edycję swoich danych.
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     PUT /api/userprofile
    ///     {
    ///        "userName": "nowy_nick",
    ///        "fullName": "Jan Kowalski",
    ///        "email": "jan.kowalski@example.com"
    ///     }
    /// 
    /// **Zasady walidacji:**
    /// * `UserName`: Unikalny w systemie, bez spacji.
    /// * `Email`: Unikalny w systemie, poprawny format.
    /// </remarks>
    /// <param name="request">Obiekt z nowymi danymi.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Zaktualizowany obiekt profilu.</returns>
    /// <response code="200">Dane zostały zaktualizowane.</response>
    /// <response code="400">
    /// Błąd walidacji lub konflikt danych.
    /// Możliwe komunikaty:
    /// * "This username is already taken."
    /// * "This email is already in use."
    /// * "UserName cannot contain spaces."
    /// </response>
    /// <response code="401">Użytkownik niezalogowany.</response>
    [HttpPut]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        // Logika serwisu rzuca InvalidOperationException przy duplikatach,
        // co Middleware zamienia na 400 Bad Request.
        var dto = await _svc.UpdateProfileAsync(request, ct);
        return Ok(dto);
    }

    /// <summary>
    /// Zmienia hasło zalogowanego użytkownika.
    /// </summary>
    /// <remarks>
    /// Wymaga podania aktualnego hasła w celu weryfikacji tożsamości.
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     POST /api/userprofile/change-password
    ///     {
    ///        "currentPassword": "StareHaslo123!",
    ///        "newPassword": "NoweSuperHaslo1!",
    ///        "confirmPassword": "NoweSuperHaslo1!"
    ///     }
    ///     
    /// **Wymagania nowego hasła:**
    /// * Minimum 8 znaków.
    /// * Przynajmniej jedna litera i jedna cyfra.
    /// * `ConfirmPassword` musi być identyczne jak `NewPassword`.
    /// </remarks>
    /// <param name="request">Dane do zmiany hasła.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="204">Hasło zostało pomyślnie zmienione.</response>
    /// <response code="400">
    /// Błąd walidacji (np. hasło za słabe) lub nieprawidłowe stare hasło.
    /// Komunikat: "Current password is incorrect."
    /// </response>
    /// <response code="401">Użytkownik niezalogowany.</response>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        await _svc.ChangePasswordAsync(request, ct);
        return NoContent();
    }

    /// <summary>
    /// Pobiera wagę docelową użytkownika.
    /// </summary>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <returns>Waga docelowa w kg (null jeśli nie ustawiona).</returns>
    /// <response code="200">Zwraca wagę docelową.</response>
    /// <response code="401">Użytkownik niezalogowany.</response>
    [HttpGet("target-weight")]
    [ProducesResponseType(typeof(TargetWeightDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TargetWeightDto>> GetTargetWeight(CancellationToken ct)
    {
        var dto = await _svc.GetTargetWeightAsync(ct);
        return Ok(dto);
    }

    /// <summary>
    /// Ustawia lub usuwa wagę docelową użytkownika.
    /// </summary>
    /// <remarks>
    /// Pozwala na:
    /// * Ustawienie wagi docelowej (40-200 kg)
    /// * Usunięcie wagi docelowej (null lub 0)
    /// 
    /// **Przykładowe żądania:**
    /// 
    ///     PUT /api/userprofile/target-weight
    ///     { "targetWeightKg": 75.5 }
    ///     
    ///     PUT /api/userprofile/target-weight
    ///     { "targetWeightKg": null }
    ///     
    ///     PUT /api/userprofile/target-weight
    ///     { "targetWeightKg": 0 }
    /// </remarks>
    /// <param name="request">Waga docelowa (40-200 kg, null lub 0 czyści).</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="204">Waga docelowa została zaktualizowana.</response>
    /// <response code="400">Błąd walidacji (waga poza zakresem 40-200 kg).</response>
    /// <response code="401">Użytkownik niezalogowany.</response>
    [HttpPut("target-weight")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateTargetWeight(
        [FromBody] UpdateTargetWeightRequest request,
        CancellationToken ct)
    {
        await _svc.UpdateTargetWeightAsync(request, ct);
        return NoContent();
    }

    /// <summary>
    /// Zmienia ustawienie widoczności danych biometrycznych dla znajomych.
    /// </summary>
    /// <remarks>
    /// Kontroluje czy znajomi mogą widzieć:
    /// * Wagę docelową
    /// * Pomiary ciała (BodyMeasurements)
    /// * Aktualna wagę
    /// 
    /// **Przykładowe żądanie:**
    /// 
    ///     PUT /api/userprofile/biometrics-privacy
    ///     { "shareWithFriends": true }
    /// </remarks>
    /// <param name="request">Ustawienie widoczności.</param>
    /// <param name="ct">Token anulowania operacji.</param>
    /// <response code="204">Ustawienie zostało zaktualizowane.</response>
    /// <response code="400">Błąd walidacji.</response>
    /// <response code="401">Użytkownik niezalogowany.</response>
    [HttpPut("biometrics-privacy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateBiometricsPrivacy(
        [FromBody] UpdateBiometricsPrivacyRequest request,
        CancellationToken ct)
    {
        await _svc.UpdateBiometricsPrivacyAsync(request.ShareWithFriends, ct);
        return NoContent();
    }
}