namespace Application.DTOs;

/// <summary>
/// Request model for responding to a shared plan.
/// </summary>
public sealed class RespondSharedPlanRequest
{
    /// <summary>
    /// Decision: true = Accept, false = Reject.
    /// </summary>
    public bool Accept { get; set; }
}