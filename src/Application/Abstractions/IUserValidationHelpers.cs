namespace Application.Abstractions;

public interface IUserValidationHelpers
{
    Task EnsureEmailIsUniqueAsync(string email, CancellationToken ct, Guid? excludeUserId = null);
    Task EnsureUserNameIsUniqueAsync(string userName, CancellationToken ct, Guid? excludeUserId = null);
}
