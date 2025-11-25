using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests.Domain;

public class DomainEntitiesTests
{
    [Fact]
    public void BodyMeasurement_ShouldInitializeCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var measurement = new BodyMeasurement
        {
            Id = id,
            UserId = userId,
            MeasuredAtUtc = now,
            WeightKg = 80.5m,
            HeightCm = 180,
            BMI = 24.8m,
            BodyFatPercentage = 15.5m,
            ChestCm = 100,
            WaistCm = 80,
            HipsCm = 95,
            BicepsCm = 35,
            ThighsCm = 60,
            Notes = "Test notes"
        };

        // Assert
        measurement.Id.Should().Be(id);
        measurement.UserId.Should().Be(userId);
        measurement.MeasuredAtUtc.Should().Be(now);
        measurement.WeightKg.Should().Be(80.5m);
        measurement.HeightCm.Should().Be(180);
        measurement.BMI.Should().Be(24.8m);
        measurement.BodyFatPercentage.Should().Be(15.5m);
        measurement.ChestCm.Should().Be(100);
        measurement.WaistCm.Should().Be(80);
        measurement.HipsCm.Should().Be(95);
        measurement.BicepsCm.Should().Be(35);
        measurement.ThighsCm.Should().Be(60);
        measurement.Notes.Should().Be("Test notes");
    }

    [Fact]
    public void Friendship_ShouldInitializeCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var friendship = new Friendship
        {
            Id = id,
            UserAId = userAId,
            UserBId = userBId,
            RequestedByUserId = requesterId,
            Status = RequestStatus.Accepted,
            CreatedAtUtc = now,
            RespondedAtUtc = now.AddHours(1)
        };

        // Assert
        friendship.Id.Should().Be(id);
        friendship.UserAId.Should().Be(userAId);
        friendship.UserBId.Should().Be(userBId);
        friendship.RequestedByUserId.Should().Be(requesterId);
        friendship.Status.Should().Be(RequestStatus.Accepted);
        friendship.CreatedAtUtc.Should().Be(now);
        friendship.RespondedAtUtc.Should().Be(now.AddHours(1));
    }

    [Fact]
    public void Plan_ShouldInitializeCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var plan = new Plan
        {
            Id = id,
            PlanName = "Test Plan",
            Type = "PPL",
            Notes = "Test Notes",
            CreatedByUserId = userId
        };

        // Assert
        plan.Id.Should().Be(id);
        plan.PlanName.Should().Be("Test Plan");
        plan.Type.Should().Be("PPL");
        plan.Notes.Should().Be("Test Notes");
        plan.CreatedByUserId.Should().Be(userId);
        plan.Exercises.Should().NotBeNull();
    }

    [Fact]
    public void ScheduledWorkout_ShouldInitializeCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var time = TimeOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var scheduled = new ScheduledWorkout
        {
            Id = id,
            Date = date,
            Time = time,
            PlanId = planId,
            PlanName = "Snapshot Plan",
            Notes = "Scheduled Notes",
            Status = ScheduledStatus.Completed,
            UserId = userId,
            IsVisibleToFriends = true
        };

        // Assert
        scheduled.Id.Should().Be(id);
        scheduled.Date.Should().Be(date);
        scheduled.Time.Should().Be(time);
        scheduled.PlanId.Should().Be(planId);
        scheduled.PlanName.Should().Be("Snapshot Plan");
        scheduled.Notes.Should().Be("Scheduled Notes");
        scheduled.Status.Should().Be(ScheduledStatus.Completed);
        scheduled.UserId.Should().Be(userId);
        scheduled.IsVisibleToFriends.Should().BeTrue();
        scheduled.Exercises.Should().NotBeNull();
    }

    [Fact]
    public void ScheduledSet_ShouldInitializeCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var exerciseId = Guid.NewGuid();

        // Act
        var set = new ScheduledSet
        {
            Id = id,
            ScheduledExerciseId = exerciseId,
            SetNumber = 1,
            Reps = 10,
            Weight = 100.5m
        };

        // Assert
        set.Id.Should().Be(id);
        set.ScheduledExerciseId.Should().Be(exerciseId);
        set.SetNumber.Should().Be(1);
        set.Reps.Should().Be(10);
        set.Weight.Should().Be(100.5m);
    }

    [Fact]
    public void SessionSet_ShouldInitializeCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var exerciseId = Guid.NewGuid();

        // Act
        var set = new SessionSet
        {
            Id = id,
            SessionExerciseId = exerciseId,
            SetNumber = 2,
            RepsPlanned = 12,
            WeightPlanned = 50m,
            RepsDone = 12,
            WeightDone = 50m,
            Rpe = 8.5m,
            IsFailure = true
        };

        // Assert
        set.Id.Should().Be(id);
        set.SessionExerciseId.Should().Be(exerciseId);
        set.SetNumber.Should().Be(2);
        set.RepsPlanned.Should().Be(12);
        set.WeightPlanned.Should().Be(50m);
        set.RepsDone.Should().Be(12);
        set.WeightDone.Should().Be(50m);
        set.Rpe.Should().Be(8.5m);
        set.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void User_ShouldInitializeCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var user = new User
        {
            Id = id,
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            FullName = "Test User",
            UserName = "testuser",
            CreatedAtUtc = now
        };

        // Assert
        user.Id.Should().Be(id);
        user.Email.Should().Be("test@example.com");
        user.PasswordHash.Should().Be("hashed_password");
        user.FullName.Should().Be("Test User");
        user.UserName.Should().Be("testuser");
        user.CreatedAtUtc.Should().Be(now);
        user.UserRoles.Should().NotBeNull();
        user.RefreshTokens.Should().NotBeNull();
        user.BodyMeasurements.Should().NotBeNull();
    }

    [Fact]
    public void WorkoutSession_ShouldInitializeCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var scheduledId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var session = new WorkoutSession
        {
            Id = id,
            ScheduledId = scheduledId,
            StartedAtUtc = now,
            CompletedAtUtc = now.AddHours(1),
            DurationSec = 3600,
            Status = WorkoutSessionStatus.Completed,
            SessionNotes = "Great session",
            UserId = userId
        };

        // Assert
        session.Id.Should().Be(id);
        session.ScheduledId.Should().Be(scheduledId);
        session.StartedAtUtc.Should().Be(now);
        session.CompletedAtUtc.Should().Be(now.AddHours(1));
        session.DurationSec.Should().Be(3600);
        session.Status.Should().Be(WorkoutSessionStatus.Completed);
        session.SessionNotes.Should().Be("Great session");
        session.UserId.Should().Be(userId);
        session.Exercises.Should().NotBeNull();
    }
}
