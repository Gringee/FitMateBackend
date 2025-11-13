namespace Domain.Entities;

public class Friendship
{
    public Guid Id { get; set; }
    
    public Guid UserAId { get; set; }
    public Guid UserBId { get; set; }
    
    public Guid RequestedByUserId { get; set; }
    
    public string Status { get; set; } = "Pending"; // Pending | Accepted | Rejected

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAtUtc { get; set; }
    
    public User? UserA { get; set; }
    public User? UserB { get; set; }
    public User? RequestedByUser { get; set; }
}