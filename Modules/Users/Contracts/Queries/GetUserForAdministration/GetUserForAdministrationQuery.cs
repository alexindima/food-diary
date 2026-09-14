using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Queries.GetUserForAdministration;

public sealed record GetUserForAdministrationQuery(
    UserId UserId) : IRequest<UserAdminReadModel?>;
