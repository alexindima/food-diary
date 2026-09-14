using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Queries.GetUserAdministrationSummary;

public sealed record GetUserAdministrationSummaryQuery(
    int RecentLimit) : IRequest<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers)>;
