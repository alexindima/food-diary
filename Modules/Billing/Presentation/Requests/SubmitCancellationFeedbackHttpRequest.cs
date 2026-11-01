using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.Billing.Presentation.Requests;

public sealed record SubmitCancellationFeedbackHttpRequest {
    [MaxLength(64)]
    public string? ReasonCode { get; init; }

    [MaxLength(2000)]
    public string? Comment { get; init; }
}
