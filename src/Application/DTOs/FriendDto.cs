using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class FriendDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;
}

public class FriendRequestDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid FromUserId { get; set; }

    [Required]
    [StringLength(100)]
    public string FromName { get; set; } = string.Empty;

    [Required]
    public Guid ToUserId { get; set; }

    [Required]
    [StringLength(100)]
    public string ToName { get; set; } = string.Empty;

    [Required]
    [RegularExpression("Pending|Accepted|Rejected")]
    public string Status { get; set; } = "Pending";

    [Required]
    public DateTime CreatedAtUtc { get; set; }

    public DateTime? RespondedAtUtc { get; set; }
}

public class RespondFriendRequest
{
    [Required]
    public bool Accept { get; set; }
}