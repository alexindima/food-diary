namespace FoodDiary.Modules.Admin.Application.Abstractions.Models;

public sealed record AdminBillingCancellationReadModel(
    Guid Id, Guid UserId, string? UserEmail, string Provider, string ExternalSubscriptionId, string? Plan,
    string Source, string Outcome, DateTime RequestedAtUtc, DateTime EffectiveAtUtc,
    string? ReasonCode, string? Comment, DateTime? FeedbackSubmittedAtUtc);
