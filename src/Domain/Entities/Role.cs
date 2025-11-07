namespace Domain.Entities;

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!; // "User", "Admin"

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}