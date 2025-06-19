using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Domain.Enums;

namespace Application.DTOs;

public class FeSetDetailsDto
{
    public int Reps { get; set; }
    public decimal Weight { get; set; }
}

public class FeExerciseDto
{

    public string Name { get; set; } = null!;
    public int Rest { get; set; }
    public List<FeSetDetailsDto> Sets { get; set; } = new();
}

public class FePlanDto
{
    public Guid Id { get; set; }
    public string Date { get; set; } = null!;   // „YYYY-MM-DD”
    public string? Time { get; set; } // „HH:mm” 
    [JsonPropertyName("planName")]
    public string PlanName { get; set; } = null!;
    public string Notes { get; set; } = null!;

    public List<FeExerciseDto> Exercises { get; set; } = new();
}

public class FeScheduledWorkoutDto
{
    public Guid Id { get; set; }
    public string Date { get; set; } = null!;   // „YYYY-MM-DD”
    public string? Time { get; set; }            // „HH:mm”
    [JsonPropertyName("planName")]
    public string PlanName { get; set; } = null!;
    public string Notes { get; set; } = null!;
    public List<FeExerciseDto> Exercises { get; set; } = new();
    public WorkoutStatus Status { get; set; }
}

public class DayWorkoutRefDto
{
    public Guid Id { get; set; }
    public string Date { get; set; } = null!;   // „YYYY-MM-DD”
}
