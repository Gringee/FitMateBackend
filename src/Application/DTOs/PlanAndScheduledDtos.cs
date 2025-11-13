namespace Application.DTOs;

public class SetDto
{
    public int Reps { get; set; }
    public decimal Weight { get; set; }
}

public class ExerciseDto
{
    public string Name { get; set; } = null!;
    public int Rest { get; set; } = 90;
    public List<SetDto> Sets { get; set; } = new();
}

public class PlanDto
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Notes { get; set; }
    public List<ExerciseDto> Exercises { get; set; } = new();
}

public class CreatePlanDto
{
    public string PlanName { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Notes { get; set; }
    public List<ExerciseDto> Exercises { get; set; } = new();
}

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

public class SharedPlanDto
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string SharedByName { get; set; } = string.Empty;
    public DateTime SharedAtUtc { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? RespondedAtUtc { get; set; }
}