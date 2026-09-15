using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserAdministrationSummary;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Application.Queries.GetUserAdministrationSummary;

public sealed class GetUserAdministrationSummaryQueryHandler(IUserAdminReadModelRepository repository) : IRequestHandler<GetUserAdministrationSummaryQuery, (int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers)> {
    public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers)> Handle(GetUserAdministrationSummaryQuery request, CancellationToken cancellationToken) {
        int recentLimit = request.RecentLimit;
        return repository.GetAdminDashboardSummaryReadModelsAsync(recentLimit, cancellationToken);
    }

}
