using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;

public sealed record ProcessBillingWebhookCommand(
    string Provider,
    string Payload,
    string SignatureHeader,
    bool QueueOnly = false)
    : IRequest<Result>, ITransactionalCommand;
