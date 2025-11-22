using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

/// <summary>
/// Represents a set within an exercise.
/// </summary>
public class SetDto
{
    [Range(1, 1000, ErrorMessage = "Reps must be between 1 and 1000.")]
    public int Reps { get; set; }
    
    [Range(0, 1000, ErrorMessage = "Weight must be between 0 and 1000.")]
    public decimal Weight { get; set; }
}

/// <summary>
/// Represents an exercise within a workout plan.
/// </summary>
public class ExerciseDto : IValidatableObject
{
    [Required, StringLength(100, MinimumLength = 1, ErrorMessage = "Exercise name is required (1-100 chars).")]
    public string Name { get; set; } = null!;
    
    [Range(0, 3600, ErrorMessage = "Rest must be between 0 and 3600 seconds.")]
    public int Rest { get; set; } = 90;
    public IReadOnlyList<SetDto> Sets { get; set; } = new List<SetDto>();

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Trim().Length == 0)
            yield return new ValidationResult("Exercise name cannot be empty/whitespace.", new[] { nameof(Name) });

        if (Sets is null || Sets.Count == 0)
            yield return new ValidationResult("Exercise must contain at least one set.", new[] { nameof(Sets) });

        if (Sets.Count > 50)
            yield return new ValidationResult("Too many sets (max 50 per exercise).", new[] { nameof(Sets) });
    }
}

/// <summary>
/// Represents a workout plan.
/// </summary>
public class PlanDto
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Notes { get; set; }
    public IReadOnlyList<ExerciseDto> Exercises { get; set; } = new List<ExerciseDto>();
}

/// <summary>
/// Data transfer object for creating a new workout plan.
/// </summary>
public sealed class CreatePlanDto : IValidatableObject
{
    [Required, StringLength(100, MinimumLength = 3, ErrorMessage = "Plan name must be 3-100 chars.")]
    public string PlanName { get; set; } = null!;
    
    [Required, StringLength(50, MinimumLength = 2, ErrorMessage = "Type must be 2-50 chars.")]
    public string Type { get; set; } = null!;

    [StringLength(1000, ErrorMessage = "Notes max length is 1000 chars.")]
    public string? Notes { get; set; }

    public List<ExerciseDto> Exercises { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (string.IsNullOrWhiteSpace(PlanName) || PlanName.Trim().Length < 3)
            yield return new ValidationResult("Plan name cannot be empty and must have at least 3 non-space characters.", new[] { nameof(PlanName) });

        if (string.IsNullOrWhiteSpace(Type) || Type.Trim().Length < 2)
            yield return new ValidationResult("Type cannot be empty and must have at least 2 non-space characters.", new[] { nameof(Type) });

        if (Exercises is null || Exercises.Count == 0)
            yield return new ValidationResult("Plan must contain at least one exercise.", new[] { nameof(Exercises) });

        if (Exercises.Count > 100)
            yield return new ValidationResult("Too many exercises (max 100 per plan).", new[] { nameof(Exercises) });
    }
}

public class SharedPlanDto
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string SharedByName { get; set; } = string.Empty;
    public string SharedWithName { get; set; } = string.Empty;
    public DateTime SharedAtUtc { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? RespondedAtUtc { get; set; }
}