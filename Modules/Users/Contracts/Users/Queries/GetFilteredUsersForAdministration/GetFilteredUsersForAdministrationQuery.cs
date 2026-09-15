using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Users.Queries.GetFilteredUsersForAdministration;

public sealed record GetFilteredUsersForAdministrationQuery(
    string? Search,
    int Page,
    int Limit,
    UserAccountStatusFilter Status,
    UserAdministrationFilter Filter) : IRequest<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)>;
