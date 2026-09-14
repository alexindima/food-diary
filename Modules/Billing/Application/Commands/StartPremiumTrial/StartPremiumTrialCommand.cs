using FoodDiary.Results;
using FoodDiary.Modules.Billing.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Billing.Application.Commands.StartPremiumTrial;

public sealed record StartPremiumTrialCommand(Guid? UserId)
    : IRequest<Result<BillingOverviewModel>>, ITransactionalCommand;
