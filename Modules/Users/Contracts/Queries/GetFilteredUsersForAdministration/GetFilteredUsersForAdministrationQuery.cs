using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Contracts.Queries.GetFilteredUsersForAdministration;

public sealed record GetFilteredUsersForAdministrationQuery(
    string? Search,
    int Page,
    int Limit,
    UserAccountStatusFilter Status,
    UserAdministrationFilter Filter) : IRequest<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)>;
