using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs;

/// ──────────────────────────────────────────────
/// Plano-/Workout-view specjalnie dla SPA
/// ──────────────────────────────────────────────
public class FeSetDetailsDto
{
    public int Reps { get; set; }
    public decimal Weight { get; set; }
}

public class FeExerciseDto
{
    public string Name { get; set; } = null!;
    public int Rest { get; set; }           // sekundy
    public List<FeSetDetailsDto> Sets { get; set; } = new();
}

public class FePlanDto             // = interface Plan
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Description { get; set; } = null!;
    public List<FeExerciseDto> Exercises { get; set; } = new();
}

public class FeScheduledWorkoutDto // = interface ScheduledWorkout
{
    public Guid Id { get; set; }
    public string Date { get; set; } = null!;   // “YYYY-MM-DD”
    public string? Time { get; set; }            // “HH:mm” opcjonalnie
    public int PlanId { get; set; }
    public string PlanName { get; set; } = null!;
    public List<FeExerciseDto> Exercises { get; set; } = new();
    public WorkoutStatus Status { get; set; }            // enum → “planned”…
}
