namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminBillingCancellationHttpResponse(
    Guid Id, Guid UserId, string? UserEmail, string Provider, string ExternalSubscriptionId, string? Plan,
    string Source, string Outcome, DateTime RequestedAtUtc, DateTime EffectiveAtUtc,
    string? ReasonCode, string? Comment, DateTime? FeedbackSubmittedAtUtc);
