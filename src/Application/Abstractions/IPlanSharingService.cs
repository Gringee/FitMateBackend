using Application.DTOs;

namespace Application.Abstractions;

/// <summary>
/// Serwis odpowiedzialny za udostępnianie planów treningowych między użytkownikami.
/// </summary>
public interface IPlanSharingService
{
    /// <summary>
    /// Udostępnia plan innemu użytkownikowi.
    /// </summary>
    Task ShareToUserAsync(Guid planId, Guid sharedWithUserId, CancellationToken ct);
    
    /// <summary>
    /// Pobiera listę planów udostępnionych zalogowanemu użytkownikowi (status Accepted).
    /// </summary>
    Task<IReadOnlyList<PlanDto>> GetSharedWithMeAsync(CancellationToken ct);
    
    /// <summary>
    /// Odpowiada na udostępnienie planu (akceptuje lub odrzuca).
    /// </summary>
    Task RespondToSharedPlanAsync(Guid sharedPlanId, bool accept, CancellationToken ct);
    
    /// <summary>
    /// Pobiera otrzymane zaproszenia do planów oczekujące na decyzję.
    /// </summary>
    Task<IReadOnlyList<SharedPlanDto>> GetPendingSharedPlansAsync(CancellationToken ct);
    
    /// <summary>
    /// Pobiera wysłane zaproszenia do plenów oczekujące na decyzję odbiorcy.
    /// </summary>
    Task<IReadOnlyList<SharedPlanDto>> GetSentPendingSharedPlansAsync(CancellationToken ct);
    
    /// <summary>
    /// Pobiera historię udostępnień (zakończone: accepted/rejected).
    /// </summary>
    /// <param name="scope">Zakres: "received", "sent", "all" (null = received)</param>
    Task<IReadOnlyList<SharedPlanDto>> GetSharedHistoryAsync(string? scope, CancellationToken ct);
    
    /// <summary>
    /// Usuwa lub anuluje udostępnienie planu.
    /// </summary>
    Task DeleteSharedPlanAsync(Guid sharedPlanId, bool onlyIfPending, CancellationToken ct = default);
}
