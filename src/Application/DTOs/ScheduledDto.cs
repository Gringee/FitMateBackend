using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

/// <summary>
/// Represents a scheduled workout.
/// </summary>
public class ScheduledDto
{
    /// <summary>
    /// Unique identifier of the scheduled workout.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Date of the scheduled workout.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Optional time of the scheduled workout.
    /// </summary>
    public TimeOnly? Time { get; set; }

    /// <summary>
    /// ID of the plan used for this workout.
    /// </summary>
    public Guid PlanId { get; set; }

    /// <summary>
    /// Name of the plan.
    /// </summary>
    public string PlanName { get; set; } = null!;

    /// <summary>
    /// Optional notes for the scheduled workout.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// List of exercises in the scheduled workout.
    /// </summary>
    public IReadOnlyList<ExerciseDto> Exercises { get; set; } = new List<ExerciseDto>();

    /// <summary>
    /// Status of the workout.
    /// Possible values: "planned", "completed".
    /// </summary>
    public string Status { get; set; } = "planned";

    /// <summary>
    /// Whether this workout is visible to friends.
    /// </summary>
    public bool VisibleToFriends { get; set; }
}

/// <summary>
/// Data transfer object for creating or updating a scheduled workout.
/// </summary>
public sealed class CreateScheduledDto
{
    /// <summary>
    /// Date of the workout.
    /// </summary>
    [Required]
    public DateOnly Date { get; set; }
    
    /// <summary>
    /// Optional time of the workout.
    /// </summary>
    public TimeOnly? Time { get; set; }
    
    /// <summary>
    /// ID of the plan to schedule.
    /// </summary>
    [Required]
    public Guid PlanId { get; set; }
    
    /// <summary>
    /// Optional name of the plan (if overriding or for display).
    /// </summary>
    [StringLength(200)]
    public string? PlanName { get; set; }

    /// <summary>
    /// Optional notes for the workout.
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// List of exercises to include.
    /// </summary>
    [MaxLength(100, ErrorMessage = "Too many exercises (max 100).")]
    public List<ExerciseDto> Exercises { get; set; } = new();

    /// <summary>
    /// Status of the workout.
    /// Possible values: "planned", "completed".
    /// </summary>
    [RegularExpression("(?i)^(planned|completed)$", ErrorMessage = "Status must be 'planned' or 'completed'.")]
    public string? Status { get; set; }
    
    /// <summary>
    /// Whether to make this workout visible to friends.
    /// </summary>
    public bool VisibleToFriends { get; set; } = false;
}