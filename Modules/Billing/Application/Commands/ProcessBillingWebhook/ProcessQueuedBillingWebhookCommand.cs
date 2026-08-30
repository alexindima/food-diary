using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed record ProcessQueuedBillingWebhookCommand(Guid WebhookEventId)
    : IRequest<Result>, ITransactionalCommand;
