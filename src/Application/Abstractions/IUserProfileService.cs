using Application.DTOs;

namespace Application.Abstractions;

public interface IUserProfileService
{
    Task<UserProfileDto> GetCurrentAsync(CancellationToken ct);
    Task<UserProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct);
}