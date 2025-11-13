namespace Application.DTOs;

public class FriendDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class FriendRequestDto
{
    public Guid Id { get; set; }
    public Guid FromUserId { get; set; }
    public string FromName { get; set; } = string.Empty;
    public Guid ToUserId { get; set; }
    public string ToName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
}

public class RespondFriendRequest
{
    public bool Accept { get; set; }
}