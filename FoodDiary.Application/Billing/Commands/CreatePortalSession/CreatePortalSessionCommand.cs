using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Billing.Commands.CreatePortalSession;

public sealed record CreatePortalSessionCommand(Guid? UserId)
    : ICommand<Result<BillingPortalSessionModel>>, IUserRequest;
