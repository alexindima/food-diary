using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Users.Queries.GetUserBillingProfile;

public sealed record GetUserBillingProfileQuery(
    UserId UserId) : IRequest<Result<UserBillingProfileModel>>;
