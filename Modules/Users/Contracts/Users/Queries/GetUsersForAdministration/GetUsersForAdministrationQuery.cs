using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Users.Queries.GetUsersForAdministration;

public sealed record GetUsersForAdministrationQuery(
    string? Search,
    int Page,
    int Limit,
    UserAccountStatusFilter Status) : IRequest<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)>;
