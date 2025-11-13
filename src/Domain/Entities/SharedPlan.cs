namespace Domain.Entities;

public class SharedPlan
{
    public Guid Id { get; set; }

    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public Guid SharedByUserId { get; set; } 
    public User SharedByUser { get; set; } = null!;

    public Guid SharedWithUserId { get; set; } 
    public User SharedWithUser { get; set; } = null!;

    public DateTime SharedAtUtc { get; set; } = DateTime.UtcNow;
    
    public string Status { get; set; } = "Pending"; // Pending | Accepted | Rejected
    public DateTime? RespondedAtUtc { get; set; }
}