using Application.Abstractions;
using Application.Common.Security; // Używamy Extension Method
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class FriendshipService : IFriendshipService
{
    private readonly IApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public FriendshipService(IApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }
    private Guid UserId => _http.HttpContext?.User.GetUserId() 
                           ?? throw new UnauthorizedAccessException();

    private static (Guid A, Guid B) CanonicalPair(Guid u1, Guid u2)
        => u1.CompareTo(u2) < 0 ? (u1, u2) : (u2, u1);

    public async Task SendRequestAsync(Guid toUserId, CancellationToken ct)
    {
        var me = UserId;
        if (me == toUserId) throw new InvalidOperationException("You cannot add yourself.");

        var (a, b) = CanonicalPair(me, toUserId);

        var existing = await _db.Friendships
            .FirstOrDefaultAsync(f => f.UserAId == a && f.UserBId == b, ct);

        if (existing != null)
        {
            if (existing.Status == RequestStatus.Accepted) throw new InvalidOperationException("You are already friends.");
            if (existing.Status == RequestStatus.Pending)
            {
                if (existing.RequestedByUserId == toUserId)
                {
                    existing.Status = RequestStatus.Accepted;
                    existing.RespondedAtUtc = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    return;
                }
                throw new InvalidOperationException("A friend request is already in progress.");
            }
            existing.Status = RequestStatus.Pending;
            existing.RequestedByUserId = me;
            existing.CreatedAtUtc = DateTime.UtcNow;
            existing.RespondedAtUtc = null;
            await _db.SaveChangesAsync(ct);
            return;
        }

        var f = new Friendship
        {
            Id = Guid.NewGuid(),
            UserAId = a,
            UserBId = b,
            RequestedByUserId = me,
            Status = RequestStatus.Pending
        };
        _db.Friendships.Add(f);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SendRequestByUserNameAsync(string toUserName, CancellationToken ct)
    {
        var normalized = toUserName.Trim().ToLowerInvariant();
        var toUser = await _db.Users.FirstOrDefaultAsync(u => u.UserName == normalized, ct);

        if (toUser is null)
            throw new KeyNotFoundException("A user with the provided username does not exist.");

        await SendRequestAsync(toUser.Id, ct);
    }

    public async Task RespondAsync(Guid requestId, bool accept, CancellationToken ct)
    {
        var me = UserId;
        var fr = await _db.Friendships.FirstOrDefaultAsync(x => x.Id == requestId, ct)
                 ?? throw new KeyNotFoundException("Friend request not found.");

        if (fr.UserAId != me && fr.UserBId != me) 
            throw new UnauthorizedAccessException();

        if (fr.Status != RequestStatus.Pending) 
            throw new InvalidOperationException("The friend request has already been processed.");
        
        if (fr.RequestedByUserId == me) 
            throw new InvalidOperationException("You cannot respond to your own friend request.");

        fr.Status = accept ? RequestStatus.Accepted : RequestStatus.Rejected;
        fr.RespondedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
    public async Task<IReadOnlyList<Guid>> GetFriendIdsAsync(CancellationToken ct)
    {
        var me = UserId;
        
        var rows = await _db.Friendships
            .AsNoTracking()
            .Where(f => f.Status == RequestStatus.Accepted && (f.UserAId == me || f.UserBId == me))
            .Select(f => new { f.UserAId, f.UserBId })
            .ToListAsync(ct);

        return rows
            .Select(r => r.UserAId == me ? r.UserBId : r.UserAId)
            .Distinct()
            .ToList();
    }

    public async Task<IReadOnlyList<FriendDto>> GetFriendsAsync(CancellationToken ct)
    {
        var friendIds = await GetFriendIdsAsync(ct); 

        if (friendIds.Count == 0)
            return Array.Empty<FriendDto>();

        return await _db.Users
            .AsNoTracking()
            .Where(u => friendIds.Contains(u.Id))
            .Select(u => new FriendDto { UserId = u.Id, UserName = u.UserName })
            .OrderBy(u => u.UserName)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FriendRequestDto>> GetIncomingAsync(CancellationToken ct)
    {
        var me = UserId;
        var meName = await GetMyNameAsync(me, ct);

        var q = from f in _db.Friendships
                join uFrom in _db.Users on f.RequestedByUserId equals uFrom.Id
                where f.Status == RequestStatus.Pending
                      && (f.UserAId == me || f.UserBId == me)
                      && f.RequestedByUserId != me
                select new FriendRequestDto
                {
                    Id = f.Id,
                    FromUserId = uFrom.Id,
                    FromName = uFrom.FullName ?? string.Empty,
                    ToUserId = me,
                    ToName = meName,
                    Status = f.Status.ToString(),
                    CreatedAtUtc = f.CreatedAtUtc,
                    RespondedAtUtc = f.RespondedAtUtc
                };

        return await q.AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FriendRequestDto>> GetOutgoingAsync(CancellationToken ct)
    {
        var me = UserId;
        var meName = await GetMyNameAsync(me, ct);

        var q = from f in _db.Friendships
                where f.Status == RequestStatus.Pending && f.RequestedByUserId == me
                join uTo in _db.Users on (f.UserAId == me ? f.UserBId : f.UserAId) equals uTo.Id
                select new FriendRequestDto
                {
                    Id = f.Id,
                    FromUserId = me,
                    FromName = meName,
                    ToUserId = uTo.Id,
                    ToName = uTo.FullName ?? string.Empty,
                    Status = f.Status.ToString(),
                    CreatedAtUtc = f.CreatedAtUtc,
                    RespondedAtUtc = f.RespondedAtUtc
                };

        return await q.AsNoTracking().ToListAsync(ct);
    }

    public async Task<bool> RemoveFriendAsync(Guid friendUserId, CancellationToken ct)
    {
        var me = UserId;
        var (a, b) = CanonicalPair(me, friendUserId);

        var fr = await _db.Friendships
            .FirstOrDefaultAsync(f => f.UserAId == a && f.UserBId == b && f.Status == RequestStatus.Accepted, ct);

        if (fr is null) return false;

        _db.Friendships.Remove(fr);
        await _db.SaveChangesAsync(ct);
        return true;
    }
    
    private async Task<string> GetMyNameAsync(Guid me, CancellationToken ct) 
        => await _db.Users.Where(x => x.Id == me).Select(x => x.FullName ?? "").FirstOrDefaultAsync(ct) ?? "";

    public async Task<bool> AreFriendsAsync(Guid userId1, Guid userId2, CancellationToken ct)
    {
        var (a, b) = CanonicalPair(userId1, userId2);
        return await _db.Friendships
            .AnyAsync(f => f.UserAId == a && f.UserBId == b && f.Status == RequestStatus.Accepted, ct);
    }

    public async Task<Guid?> GetFriendshipIdAsync(Guid userId1, Guid userId2, CancellationToken ct)
    {
        var (a, b) = CanonicalPair(userId1, userId2);
        var friendship = await _db.Friendships
            .FirstOrDefaultAsync(f => f.UserAId == a && f.UserBId == b, ct);
        return friendship?.Id;
    }
}