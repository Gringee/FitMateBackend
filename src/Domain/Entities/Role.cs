namespace Domain.Entities;

/// <summary>
/// Represents a user role (e.g., User, Admin).
/// </summary>
public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!; // "User", "Admin"

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}