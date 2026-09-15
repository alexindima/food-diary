using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfile;

public sealed record GetUserBillingProfileQuery(
    UserId UserId) : IRequest<Result<UserBillingProfileModel>>;
