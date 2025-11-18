using Application.DTOs;

namespace Application.Abstractions;

public interface IFriendshipService
{
    Task SendRequestAsync(Guid toUserId, CancellationToken ct);
    Task SendRequestByUserNameAsync(string toUserName, CancellationToken ct);
    Task RespondAsync(Guid requestId, bool accept, CancellationToken ct);
    Task<IReadOnlyList<FriendDto>> GetFriendsAsync(CancellationToken ct);
    Task<IReadOnlyList<FriendRequestDto>> GetIncomingAsync(CancellationToken ct);
    Task<IReadOnlyList<FriendRequestDto>> GetOutgoingAsync(CancellationToken ct);
    Task<bool> RemoveFriendAsync(Guid friendUserId, CancellationToken ct);
    Task<IReadOnlyList<Guid>> GetFriendIdsAsync(CancellationToken ct);
}