using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Users.Queries.GetUserAdministrationSummary;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Users.Queries.GetUserAdministrationSummary;

public sealed class GetUserAdministrationSummaryQueryHandler(IUserAdminReadModelRepository repository) : IRequestHandler<GetUserAdministrationSummaryQuery, (int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers)> {
    public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers)> Handle(GetUserAdministrationSummaryQuery request, CancellationToken cancellationToken) {
        int recentLimit = request.RecentLimit;
        return repository.GetAdminDashboardSummaryReadModelsAsync(recentLimit, cancellationToken);
    }

}
