using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Billing.Application.Commands.SubmitCancellationFeedback;

public sealed record SubmitCancellationFeedbackCommand(Guid? UserId, BillingCancellationId CancellationId, string? ReasonCode, string? Comment) : IRequest<Result>;
