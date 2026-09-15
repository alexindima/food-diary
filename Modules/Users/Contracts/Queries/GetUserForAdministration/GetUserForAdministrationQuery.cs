using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Contracts.Queries.GetUserForAdministration;

public sealed record GetUserForAdministrationQuery(
    UserId UserId) : IRequest<UserAdminReadModel?>;
