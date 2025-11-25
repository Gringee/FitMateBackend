namespace Domain.Entities;

using Domain.Enums;

/// <summary>
/// Represents a friendship relationship between two users.
/// </summary>
public class Friendship
{
    /// <summary>
    /// Unique identifier of the friendship record.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// ID of the first user in the friendship (lexicographically smaller).
    /// </summary>
    public Guid UserAId { get; set; }

    /// <summary>
    /// ID of the second user in the friendship.
    /// </summary>
    public Guid UserBId { get; set; }
    
    /// <summary>
    /// ID of the user who initiated the friendship request.
    /// </summary>
    public Guid RequestedByUserId { get; set; }
    
    /// <summary>
    /// Current status of the friendship request.
    /// </summary>
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    /// <summary>
    /// Date and time when the request was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when the request was responded to.
    /// </summary>
    public DateTime? RespondedAtUtc { get; set; }
    
    /// <summary>
    /// Navigation property to User A.
    /// </summary>
    public User UserA { get; set; } = null!;

    /// <summary>
    /// Navigation property to User B.
    /// </summary>
    public User UserB { get; set; } = null!;

    /// <summary>
    /// Navigation property to the requester.
    /// </summary>
    public User RequestedByUser { get; set; } = null!;
}