using FoodDiary.Results;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Billing.Commands.StartPremiumTrial;

public sealed record StartPremiumTrialCommand(Guid? UserId)
    : IRequest<Result<BillingOverviewModel>>, ITransactionalCommand;
