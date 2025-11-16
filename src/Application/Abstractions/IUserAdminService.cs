namespace Application.Abstractions;

public enum AssignRoleResult
{
    Ok,
    UserNotFound,
    RoleNotFound,
    AlreadyHasRole
}

public enum DeleteUserResult
{
    Ok,
    NotFound,
    IsAdmin
}

public enum ResetPasswordResult
{
    Ok,
    NotFound
}

public interface IUserAdminService
{
    Task<AssignRoleResult> AssignRoleAsync(Guid userId, string roleName, CancellationToken ct);
    Task<DeleteUserResult> DeleteUserAsync(Guid userId, CancellationToken ct);
    Task<ResetPasswordResult> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct);
}