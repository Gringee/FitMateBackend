namespace Domain.Entities;

using Domain.Enums;

/// <summary>
/// Represents a friendship relationship between two users.
/// </summary>
public class Friendship
{
    public Guid Id { get; set; }
    
    public Guid UserAId { get; set; }
    public Guid UserBId { get; set; }
    
    public Guid RequestedByUserId { get; set; }
    
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAtUtc { get; set; }
    
    public User UserA { get; set; } = null!;
    public User UserB { get; set; } = null!;
    public User RequestedByUser { get; set; } = null!;
}