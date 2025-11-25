using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

/// <summary>
/// Represents a friend in the user's friend list.
/// </summary>
public class FriendDto
{
    /// <summary>
    /// Unique identifier of the friend user.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Username of the friend.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// Represents a friendship request between two users.
/// </summary>
public sealed class FriendRequestDto
{
    /// <summary>
    /// Unique identifier of the friendship request.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the user who sent the request.
    /// </summary>
    [Required]
    public Guid FromUserId { get; set; }

    /// <summary>
    /// Username of the requester.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// ID of the user who received the request.
    /// </summary>
    [Required]
    public Guid ToUserId { get; set; }

    /// <summary>
    /// Username of the recipient.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ToName { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the request.
    /// Possible values: "Pending", "Accepted", "Rejected".
    /// </summary>
    [Required]
    [RegularExpression("Pending|Accepted|Rejected")]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Date and time when the request was created.
    /// </summary>
    [Required]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Date and time when the request was responded to.
    /// </summary>
    public DateTime? RespondedAtUtc { get; set; }
}

/// <summary>
/// Request to respond to a friendship request.
/// </summary>
public sealed class RespondFriendRequest
{
    /// <summary>
    /// Whether to accept (true) or reject (false) the friendship request.
    /// </summary>
    [Required]
    public bool Accept { get; set; }
}