using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Contracts.Email.Commands.QueueSubscriptionCancellationEmail;

// The caller owns the transaction that commits the envelope and its durable intent.
public sealed record QueueSubscriptionCancellationEmailCommand(
    string ToEmail,
    string? Language,
    string? TimeZoneId,
    DateTime? AccessUntilUtc,
    string CorrelationId) : IRequest<Result>;
