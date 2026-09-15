using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted;

public sealed record GetUserBillingProfileIncludingDeletedQuery(
    UserId UserId) : IRequest<UserBillingProfileModel?>;
