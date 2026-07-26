using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Billing.Commands.CreatePortalSession;

public sealed record CreatePortalSessionCommand(Guid? UserId)
    : IRequest<Result<BillingPortalSessionModel>>, ITransactionalCommand;
