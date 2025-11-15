using System.Security.Claims;
using Application.Abstractions;
using Application.DTOs;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public class FriendshipService : IFriendshipService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public FriendshipService(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    private Guid CurrentUserId()
    {
        var id = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id))
            throw new UnauthorizedAccessException();
        return Guid.Parse(id);
    }

    private static (Guid A, Guid B) CanonicalPair(Guid u1, Guid u2)
        => u1.CompareTo(u2) < 0 ? (u1, u2) : (u2, u1);

    public async Task SendRequestAsync(Guid toUserId, CancellationToken ct)
    {
        var me = CurrentUserId();
        if (me == toUserId) throw new InvalidOperationException("Nie mozna dodac siebie.");

        var (a, b) = CanonicalPair(me, toUserId);

        var existing = await _db.Friendships
            .FirstOrDefaultAsync(f => f.UserAId == a && f.UserBId == b, ct);

        if (existing != null)
        {
            if (existing.Status == "Accepted") throw new InvalidOperationException("Juz jestescie znajomymi.");
            if (existing.Status == "Pending")
            {
                if (existing.RequestedByUserId == toUserId)
                {
                    existing.Status = "Accepted";
                    existing.RespondedAtUtc = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    return;
                }
                throw new InvalidOperationException("Zaproszenie jest juz w toku.");
            }
            existing.Status = "Pending";
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
            Status = "Pending"
        };
        _db.Friendships.Add(f);
        await _db.SaveChangesAsync(ct);
    }
    
    public async Task SendRequestByUserNameAsync(string toUserName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(toUserName))
            throw new ArgumentException("Username jest wymagany.", nameof(toUserName));

        var normalized = toUserName.Trim();
        
        var toUser = await _db.Users
            .FirstOrDefaultAsync(u => u.UserName == normalized, ct);

        if (toUser is null)
            throw new KeyNotFoundException("Użytkownik o podanym username nie istnieje.");
        
        await SendRequestAsync(toUser.Id, ct);
    }

    public async Task RespondAsync(Guid requestId, bool accept, CancellationToken ct)
    {
        var me = CurrentUserId();
        var fr = await _db.Friendships.FirstOrDefaultAsync(x => x.Id == requestId, ct)
                 ?? throw new KeyNotFoundException("Nie znaleziono zaproszenia.");
        
        var meIsParticipant = fr.UserAId == me || fr.UserBId == me;
        if (!meIsParticipant) throw new UnauthorizedAccessException();

        if (fr.Status != "Pending") throw new InvalidOperationException("Zaproszenie juz rozpatrzone.");
        if (fr.RequestedByUserId == me) throw new InvalidOperationException("Nie mozesz odpowiadac na wlasne zaproszenie.");

        fr.Status = accept ? "Accepted" : "Rejected";
        fr.RespondedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<FriendDto>> GetFriendsAsync(CancellationToken ct)
    {
        var me = CurrentUserId();

        var rows = await _db.Friendships
            .Where(f => f.Status == "Accepted" && (f.UserAId == me || f.UserBId == me))
            .Select(f => new { f.UserAId, f.UserBId })
            .ToListAsync(ct);

        var friendIds = rows
            .Select(r => r.UserAId == me ? r.UserBId : r.UserAId)
            .Distinct()
            .ToList();

        var friends = await _db.Users
            .Where(u => friendIds.Contains(u.Id))
            .AsNoTracking()
            .Select(u => new FriendDto { UserId = u.Id, UserName = u.UserName })
            .OrderBy(u => u.UserName)
            .ToListAsync(ct);

        return friends;
    }

    public async Task<IReadOnlyList<FriendRequestDto>> GetIncomingAsync(CancellationToken ct)
    {
        var me = CurrentUserId();

        var q = from f in _db.Friendships
                join uFrom in _db.Users on f.RequestedByUserId equals uFrom.Id
                join uA in _db.Users on f.UserAId equals uA.Id
                join uB in _db.Users on f.UserBId equals uB.Id
                where f.Status == "Pending" && (f.UserAId == me || f.UserBId == me) && f.RequestedByUserId != me
                select new FriendRequestDto
                {
                    Id = f.Id,
                    FromUserId = uFrom.Id,
                    FromName = uFrom.FullName ?? string.Empty,
                    ToUserId = me,
                    ToName = _db.Users.Where(x => x.Id == me).Select(x => x.FullName).FirstOrDefault()!,
                    Status = f.Status,
                    CreatedAtUtc = f.CreatedAtUtc,
                    RespondedAtUtc = f.RespondedAtUtc
                };

        return await q.AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FriendRequestDto>> GetOutgoingAsync(CancellationToken ct)
    {
        var me = CurrentUserId();

        var q = from f in _db.Friendships
                join uTo in _db.Users on (f.UserAId == me ? f.UserBId : f.UserAId) equals uTo.Id
                where f.Status == "Pending" && f.RequestedByUserId == me
                select new FriendRequestDto
                {
                    Id = f.Id,
                    FromUserId = me,
                    FromName = _db.Users.Where(x => x.Id == me).Select(x => x.FullName).FirstOrDefault()!,
                    ToUserId = uTo.Id,
                    ToName = uTo.FullName ?? string.Empty,
                    Status = f.Status,
                    CreatedAtUtc = f.CreatedAtUtc,
                    RespondedAtUtc = f.RespondedAtUtc
                };

        return await q.AsNoTracking().ToListAsync(ct);
    }

    public async Task<bool> RemoveFriendAsync(Guid friendUserId, CancellationToken ct)
    {
        var me = CurrentUserId();
        var (a, b) = CanonicalPair(me, friendUserId);

        var fr = await _db.Friendships
            .FirstOrDefaultAsync(f => f.UserAId == a && f.UserBId == b && f.Status == "Accepted", ct);

        if (fr is null) return false;
        
        _db.Friendships.Remove(fr);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}