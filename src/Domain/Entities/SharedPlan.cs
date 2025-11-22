namespace Domain.Entities;

using Domain.Enums;

/// <summary>
/// Represents a workout plan shared between users.
/// </summary>
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
    
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public DateTime? RespondedAtUtc { get; set; }
}