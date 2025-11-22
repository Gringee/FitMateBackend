using System.ComponentModel.DataAnnotations;
using Application.Common;

namespace Application.DTOs;

public sealed class SessionsByRangeRequest : IValidatableObject
{
    [Required]
    public DateTime FromUtc { get; set; }

    [Required]
    public DateTime ToUtc { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        var from = FromUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(FromUtc, DateTimeKind.Utc)
            : FromUtc;

        var to = ToUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(ToUtc, DateTimeKind.Utc)
            : ToUtc;

        if (to <= from)
        {
            yield return new ValidationResult(
                "Parameter 'ToUtc' must be greater than 'FromUtc'.",
                new[] { nameof(ToUtc), nameof(FromUtc) });
        }
    }

    public (DateTime From, DateTime To) NormalizeToUtc() => DateHelpers.NormalizeRange(FromUtc, ToUtc);
}