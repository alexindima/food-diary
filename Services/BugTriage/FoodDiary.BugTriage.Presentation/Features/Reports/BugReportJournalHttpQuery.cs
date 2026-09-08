using System.ComponentModel.DataAnnotations;

namespace FoodDiary.BugTriage.Presentation.Features.Reports;

public sealed record BugReportJournalHttpQuery([Range(1, 10000)] int Page = 1, [Range(1, 100)] int Limit = 50,
    DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, [MaxLength(32)] string? Status = null,
    [MaxLength(320)] string? Search = null, Guid? Id = null) : IValidatableObject {
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if (FromUtc.HasValue && ToUtc.HasValue && FromUtc >= ToUtc) {
            yield return new ValidationResult("The start must precede the exclusive end.", [nameof(FromUtc), nameof(ToUtc)]);
        }
    }
}
