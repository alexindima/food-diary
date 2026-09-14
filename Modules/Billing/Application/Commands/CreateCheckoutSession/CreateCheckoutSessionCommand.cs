using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Commands.CreateCheckoutSession;

public sealed record CreateCheckoutSessionCommand(
    Guid? UserId,
    string Plan,
    string? Provider,
    string? IdempotencyKey = null)
    : IRequest<Result<BillingCheckoutSessionModel>>, ITransactionalCommand;
