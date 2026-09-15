using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Contracts.Queries.GetUsersForAdministration;

public sealed record GetUsersForAdministrationQuery(
    string? Search,
    int Page,
    int Limit,
    UserAccountStatusFilter Status) : IRequest<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)>;
