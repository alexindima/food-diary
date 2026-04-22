using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Billing.Commands.CreateCheckoutSession;

public sealed record CreateCheckoutSessionCommand(Guid? UserId, string Plan)
    : ICommand<Result<BillingCheckoutSessionModel>>, IUserRequest;
