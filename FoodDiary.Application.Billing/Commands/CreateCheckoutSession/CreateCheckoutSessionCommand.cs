using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Billing.Commands.CreateCheckoutSession;

public sealed record CreateCheckoutSessionCommand(Guid? UserId, string Plan, string? Provider)
    : IRequest<Result<BillingCheckoutSessionModel>>, ITransactionalCommand;
