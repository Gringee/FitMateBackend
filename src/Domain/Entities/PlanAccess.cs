namespace Domain.Entities;

public class PlanAccess
{
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string Permission { get; set; } = "viewer"; // np. owner|editor|viewer

    public User User { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
}