namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminBillingWebhookEventReadModel(
    Guid Id,
    string Provider,
    string EventId,
    string EventType,
    string? ExternalObjectId,
    string Status,
    DateTime ProcessedAtUtc,
    string? PayloadJson,
    string? ErrorMessage,
    DateTime CreatedOnUtc,
    DateTime? ModifiedOnUtc);
