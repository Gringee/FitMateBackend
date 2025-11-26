using Application.Abstractions;
using Application.DTOs.BodyMetrics;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class BodyMetricsService : IBodyMetricsService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFriendshipService _friends;

    public BodyMetricsService(
        IApplicationDbContext db, 
        ICurrentUserService currentUser,
        IFriendshipService friends)
    {
        _db = db;
        _currentUser = currentUser;
        _friends = friends;
    }

    public async Task<BodyMeasurementDto> AddMeasurementAsync(
        CreateBodyMeasurementDto dto, 
        CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        // Calculate BMI
        var heightM = dto.HeightCm / 100m;
        var bmi = dto.WeightKg / (heightM * heightM);

        var measurement = new BodyMeasurement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MeasuredAtUtc = DateTime.UtcNow,
            WeightKg = dto.WeightKg,
            HeightCm = dto.HeightCm,
            BMI = Math.Round(bmi, 2),
            BodyFatPercentage = dto.BodyFatPercentage,
            ChestCm = dto.ChestCm,
            WaistCm = dto.WaistCm,
            HipsCm = dto.HipsCm,
            BicepsCm = dto.BicepsCm,
            ThighsCm = dto.ThighsCm,
            Notes = dto.Notes?.Trim()
        };

        _db.BodyMeasurements.Add(measurement);
        await _db.SaveChangesAsync(ct);

        return MapToDto(measurement);
    }

    public async Task<IReadOnlyList<BodyMeasurementDto>> GetMeasurementsAsync(
        DateTime? from = null, 
        DateTime? to = null, 
        CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var query = _db.BodyMeasurements
            .Where(bm => bm.UserId == userId);

        if (from.HasValue)
            query = query.Where(bm => bm.MeasuredAtUtc >= from.Value);

        if (to.HasValue)
            query = query.Where(bm => bm.MeasuredAtUtc <= to.Value);

        var measurements = await query
            .OrderByDescending(bm => bm.MeasuredAtUtc)
            .ToListAsync(ct);

        return measurements.Select(MapToDto).ToList();
    }

    public async Task<BodyMeasurementDto?> GetLatestMeasurementAsync(
        CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        return await GetCompositeMeasurementAsync(userId, ct);
    }

    public async Task<BodyMetricsStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        // Note: For stats, we might want the raw latest measurement for accurate weight change,
        // or the composite one. Usually stats like "Current Weight" should be the latest recorded.
        // "Current BMI" also. 
        // If we use GetCompositeMeasurementAsync, we get filled-in body fat etc, but Weight/Height/BMI 
        // are still from the latest record. So it is safe to use.
        var latest = await GetCompositeMeasurementAsync(userId, ct);
        
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var oldMeasurement = await _db.BodyMeasurements
            .Where(bm => bm.UserId == userId && bm.MeasuredAtUtc >= thirtyDaysAgo)
            .OrderBy(bm => bm.MeasuredAtUtc)
            .FirstOrDefaultAsync(ct);

        var allMeasurements = await _db.BodyMeasurements
            .Where(bm => bm.UserId == userId)
            .ToListAsync(ct);

        decimal? weightChange = null;
        if (latest != null && oldMeasurement != null)
        {
            weightChange = latest.WeightKg - oldMeasurement.WeightKg;
        }

        return new BodyMetricsStatsDto(
            CurrentWeightKg: latest?.WeightKg,
            CurrentBMI: latest?.BMI,
            BMICategory: latest != null ? GetBMICategory(latest.BMI) : null,
            WeightChangeLast30Days: weightChange,
            LowestWeight: allMeasurements.Any() ? allMeasurements.Min(m => m.WeightKg) : null,
            HighestWeight: allMeasurements.Any() ? allMeasurements.Max(m => m.WeightKg) : null,
            TotalMeasurements: allMeasurements.Count
        );
    }

    public async Task<IReadOnlyList<BodyMetricsProgressDto>> GetProgressAsync(
        DateTime from, 
        DateTime to, 
        CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var measurements = await _db.BodyMeasurements
            .Where(bm => bm.UserId == userId 
                && bm.MeasuredAtUtc >= from 
                && bm.MeasuredAtUtc <= to)
            .OrderBy(bm => bm.MeasuredAtUtc)
            .Select(bm => new BodyMetricsProgressDto(
                bm.MeasuredAtUtc.Date,
                bm.WeightKg,
                bm.BMI))
            .ToListAsync(ct);

        return measurements;
    }

    public async Task DeleteMeasurementAsync(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var measurement = await _db.BodyMeasurements
            .FirstOrDefaultAsync(bm => bm.Id == id && bm.UserId == userId, ct);

        if (measurement == null)
            throw new KeyNotFoundException("Measurement not found");

        _db.BodyMeasurements.Remove(measurement);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<BodyMeasurementDto?> GetFriendMetricsAsync(
        Guid friendId, 
        CancellationToken ct = default)
    {
        var currentUserId = _currentUser.UserId;

        // 1. Check if they are friends
        var areFriends = await _friends.AreFriendsAsync(currentUserId, friendId, ct);
        if (!areFriends)
        {
            throw new KeyNotFoundException("User is not your friend.");
        }

        // 2. Check if friend shares biometrics
        var friend = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == friendId, ct);

        if (friend == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (!friend.ShareBiometricsWithFriends)
        {
            return null;
        }

        // 3. Get composite metrics
        return await GetCompositeMeasurementAsync(friendId, ct);
    }

    private async Task<BodyMeasurementDto?> GetCompositeMeasurementAsync(
        Guid userId, 
        CancellationToken ct)
    {
        var measurements = await _db.BodyMeasurements
            .AsNoTracking()
            .Where(bm => bm.UserId == userId)
            .OrderByDescending(bm => bm.MeasuredAtUtc)
            .ToListAsync(ct);

        if (!measurements.Any())
            return null;

        var composite = measurements.First();
        
        // Fill missing fields from older measurements
        foreach (var older in measurements.Skip(1))
        {
            composite.BodyFatPercentage ??= older.BodyFatPercentage;
            composite.ChestCm ??= older.ChestCm;
            composite.WaistCm ??= older.WaistCm;
            composite.HipsCm ??= older.HipsCm;
            composite.BicepsCm ??= older.BicepsCm;
            composite.ThighsCm ??= older.ThighsCm;
        }

        return MapToDto(composite);
    }

    private static BodyMeasurementDto MapToDto(BodyMeasurement m) => new(
        m.Id,
        m.MeasuredAtUtc,
        m.WeightKg,
        m.HeightCm,
        m.BMI,
        m.BodyFatPercentage,
        m.ChestCm,
        m.WaistCm,
        m.HipsCm,
        m.BicepsCm,
        m.ThighsCm,
        m.Notes
    );

    private static string GetBMICategory(decimal bmi)
    {
        return bmi switch
        {
            < 18.5m => "Underweight",
            < 25m => "Normal",
            < 30m => "Overweight",
            _ => "Obese"
        };
    }
}
