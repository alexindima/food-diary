using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Billing.Commands.StartPremiumTrial;

public sealed record StartPremiumTrialCommand(Guid? UserId) : ICommand<Result<BillingOverviewModel>>;
