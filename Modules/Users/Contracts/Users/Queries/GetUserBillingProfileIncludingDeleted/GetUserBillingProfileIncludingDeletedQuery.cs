using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Users.Queries.GetUserBillingProfileIncludingDeleted;

public sealed record GetUserBillingProfileIncludingDeletedQuery(
    UserId UserId) : IRequest<UserBillingProfileModel?>;
