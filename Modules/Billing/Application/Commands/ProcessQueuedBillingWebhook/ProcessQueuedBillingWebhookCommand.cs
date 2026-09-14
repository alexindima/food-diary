using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Commands.ProcessQueuedBillingWebhook;

// The handler commits business effects and failure metadata independently.
public sealed record ProcessQueuedBillingWebhookCommand(Guid WebhookEventId) : IRequest<Result>;
