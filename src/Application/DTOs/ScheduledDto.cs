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

public class CreateScheduledDto
{
    public string Date { get; set; } = null!;
    public string? Time { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = null!;
    public string? Notes { get; set; }
    public List<ExerciseDto> Exercises { get; set; } = new();
    public string? Status { get; set; } // optional, default planned
}