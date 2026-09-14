using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.Billing.Application.Commands.CreatePortalSession;

public sealed record CreatePortalSessionCommand(Guid? UserId)
    : IRequest<Result<BillingPortalSessionModel>>, ITransactionalCommand;
