using Application.DTOs;

namespace Application.Abstractions;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(string? search, CancellationToken ct);
    Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct);
    Task UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken ct);
    Task<IReadOnlyList<UserSummaryDto>> SearchAsync(string filter, CancellationToken ct);
}