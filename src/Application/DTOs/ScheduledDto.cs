using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

/// <summary>
/// Represents a scheduled workout.
/// </summary>
public class ScheduledDto
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = null!;
    public string? Notes { get; set; }
    public IReadOnlyList<ExerciseDto> Exercises { get; set; } = new List<ExerciseDto>();
    public string Status { get; set; } = "planned";
    public bool VisibleToFriends { get; set; }
}

/// <summary>
/// Data transfer object for creating or updating a scheduled workout.
/// </summary>
public sealed class CreateScheduledDto
{
    [Required]
    public DateOnly Date { get; set; }
    
    public TimeOnly? Time { get; set; }
    
    [Required]
    public Guid PlanId { get; set; }
    
    [StringLength(200)]
    public string? PlanName { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
    
    [MaxLength(100, ErrorMessage = "Too many exercises (max 100).")]
    public List<ExerciseDto> Exercises { get; set; } = new();

    [RegularExpression("(?i)^(planned|completed)$", ErrorMessage = "Status must be 'planned' or 'completed'.")]
    public string? Status { get; set; }
    
    public bool VisibleToFriends { get; set; } = false;
}