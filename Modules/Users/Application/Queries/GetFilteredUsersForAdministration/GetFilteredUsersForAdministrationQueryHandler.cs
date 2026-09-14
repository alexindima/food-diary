using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Queries.GetFilteredUsersForAdministration;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Users.Queries.GetFilteredUsersForAdministration;

public sealed class GetFilteredUsersForAdministrationQueryHandler(IUserAdminReadModelRepository repository) : IRequestHandler<GetFilteredUsersForAdministrationQuery, (IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> {
    public Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> Handle(GetFilteredUsersForAdministrationQuery request, CancellationToken cancellationToken) {
        string? search = request.Search;
        int page = request.Page;
        int limit = request.Limit;
        UserAccountStatusFilter status = request.Status;
        UserAdministrationFilter filter = request.Filter;
        return repository.GetFilteredPagedReadModelsAsync(search, page, limit, status, filter, cancellationToken);
    }

}
