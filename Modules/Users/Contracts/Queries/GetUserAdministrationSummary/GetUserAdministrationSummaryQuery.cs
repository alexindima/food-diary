using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Contracts.Queries.GetUserAdministrationSummary;

public sealed record GetUserAdministrationSummaryQuery(
    int RecentLimit) : IRequest<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers)>;
