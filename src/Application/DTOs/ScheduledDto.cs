using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class ScheduledDto
{
    public Guid Id { get; set; }
    public string Date { get; set; } = null!;   // "YYYY-MM-DD"
    public string? Time { get; set; }           // "HH:mm"
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = null!;
    public string? Notes { get; set; }
    public List<ExerciseDto> Exercises { get; set; } = new();
    public string Status { get; set; } = "planned"; // "planned" | "completed"
}

public class CreateScheduledDto : IValidatableObject
{
    [Required(ErrorMessage = "Date is required.")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Date must be in format yyyy-MM-dd.")]
    public string Date { get; set; } = null!;
    
    [StringLength(5, ErrorMessage = "Time must be in format HH:mm.")]
    public string? Time { get; set; }

    [Required(ErrorMessage = "PlanId is required.")]
    public Guid PlanId { get; set; }
    
    [StringLength(200, ErrorMessage = "PlanName max length is 200 chars.")]
    public string? PlanName { get; set; }

    [StringLength(1000, ErrorMessage = "Notes max length is 1000 chars.")]
    public string? Notes { get; set; }
    
    public List<ExerciseDto> Exercises { get; set; } = new();
    public string? Status { get; set; } //default planned

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (!DateOnly.TryParseExact(Date, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out DateOnly _))
        {
            yield return new ValidationResult(
                "Date must be in format yyyy-MM-dd.",
                new[] { nameof(Date) });
        }
        
        if (!string.IsNullOrWhiteSpace(Time))
        {
            if (!DateOnly.TryParseExact(Date, "yyyy-MM-dd", null,
                    System.Globalization.DateTimeStyles.None, out DateOnly _))
            {
                yield return new ValidationResult(
                    "Time must be in format HH:mm.",
                    new[] { nameof(Time) });
            }
        }
        
        if (!string.IsNullOrWhiteSpace(Status))
        {
            var s = Status.Trim().ToLowerInvariant();
            if (s is not ("planned" or "completed"))
            {
                yield return new ValidationResult(
                    "Status must be either 'planned' or 'completed'.",
                    new[] { nameof(Status) });
            }
        }
        
        if (Exercises is { Count: > 0 } && Exercises.Count > 100)
        {
            yield return new ValidationResult(
                "Too many exercises (max 100 per scheduled workout).",
                new[] { nameof(Exercises) });
        }
    }
}