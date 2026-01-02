using Application.DTOs;

namespace Application.Abstractions;

public interface IUserProfileService
{
    Task<UserProfileDto> GetCurrentAsync(CancellationToken ct);
    Task<UserProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct);
    Task<TargetWeightDto> GetTargetWeightAsync(CancellationToken ct);
    Task UpdateTargetWeightAsync(UpdateTargetWeightRequest request, CancellationToken ct);
    Task UpdateBiometricsPrivacyAsync(bool shareWithFriends, CancellationToken ct);
    Task DeleteAccountAsync(DeleteAccountDto dto, CancellationToken ct);
}