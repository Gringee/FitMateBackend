namespace Application.DTOs.Analytics;

public class OverviewDto
{
    public decimal TotalVolume { get; set; }        // suma (reps*weight) po wykonaniu
    public double AvgIntensity { get; set; }        // średnia weightDone po wykonaniu
    public int SessionsCount { get; set; }          // liczba sesji w zakresie
    public double AdherencePct { get; set; }        // completed/planned w %
    public int NewPrs { get; set; }                 // placeholder (na razie 0)
}

public class TimePointDto
{
    public string Period { get; set; } = default!;  // np. "2025-11-01" lub "2025-W45"
    public decimal Value { get; set; }              // np. volume
    public string? ExerciseName { get; set; }       // przy groupBy=exercise
}

public class E1rmPointDto
{
    public DateOnly Day { get; set; }
    public decimal E1Rm { get; set; }
    public Guid? SessionId { get; set; }
}

public class AdherenceDto
{
    public int Planned { get; set; }
    public int Completed { get; set; }
    public int Missed => Planned - Completed < 0 ? 0 : Planned - Completed;
    public double AdherencePct => Planned == 0 ? 0 : Math.Round((double)Completed / Planned * 100, 1);
}

public class PlanVsActualItemDto
{
    public string ExerciseName { get; set; } = default!;
    public int SetNumber { get; set; }
    public int RepsPlanned { get; set; }
    public decimal WeightPlanned { get; set; }
    public int? RepsDone { get; set; }
    public decimal? WeightDone { get; set; }
    public decimal? Rpe { get; set; }
    public bool? IsFailure { get; set; }
    public int RepsDiff => (RepsDone ?? 0) - RepsPlanned;
    public decimal WeightDiff => (WeightDone ?? 0) - WeightPlanned;
}