namespace Domain.Entities;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier of the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Email address of the user.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Hashed password of the user.
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// Full name of the user.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Unique username.
    /// </summary>
    public string UserName { get; set; } = null!;

    /// <summary>
    /// Date and time when the user was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Roles assigned to the user.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Refresh tokens for the user.
    /// </summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>
    /// Body measurements recorded by the user.
    /// </summary>
    public ICollection<BodyMeasurement> BodyMeasurements { get; set; } = new List<BodyMeasurement>();
}