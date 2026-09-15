using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUsersForAdministration;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Application.Queries.GetUsersForAdministration;

public sealed class GetUsersForAdministrationQueryHandler(IUserAdminReadModelRepository repository) : IRequestHandler<GetUsersForAdministrationQuery, (IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> {
    public Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> Handle(GetUsersForAdministrationQuery request, CancellationToken cancellationToken) {
        string? search = request.Search;
        int page = request.Page;
        int limit = request.Limit;
        UserAccountStatusFilter status = request.Status;
        return repository.GetPagedReadModelsAsync(search, page, limit, status, cancellationToken);
    }

}
