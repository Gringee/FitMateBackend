namespace Domain.Entities;

/// <summary>
/// Represents the many-to-many relationship between users and roles.
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}