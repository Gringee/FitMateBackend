using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

/// <summary>
/// Represents the user's target weight.
/// </summary>
public sealed class TargetWeightDto
{
    /// <summary>
    /// Target weight goal in kilograms.
    /// </summary>
    public decimal? TargetWeightKg { get; set; }
}

/// <summary>
/// Request to update target weight.
/// </summary>
public sealed class UpdateTargetWeightRequest : IValidatableObject
{
    /// <summary>
    /// Target weight goal in kilograms (40-200 kg). Use null or 0 to clear.
    /// </summary>
    public decimal? TargetWeightKg { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TargetWeightKg.HasValue)
        {
            var value = TargetWeightKg.Value;
            
            // Allow 0 to clear, otherwise validate range
            if (value != 0 && (value < 40 || value > 200))
            {
                yield return new ValidationResult(
                    "Target weight must be 0 (to clear) or between 40-200 kg",
                    new[] { nameof(TargetWeightKg) });
            }
        }
    }
}

/// <summary>
/// Request to update biometrics privacy setting.
/// </summary>
public sealed class UpdateBiometricsPrivacyRequest
{
    /// <summary>
    /// Whether to share biometric data with friends.
    /// </summary>
    [Required]
    public bool ShareWithFriends { get; set; }
}
