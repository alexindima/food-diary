using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Queries.GetUsersForAdministration;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Users.Queries.GetUsersForAdministration;

public sealed class GetUsersForAdministrationQueryHandler(IUserAdminReadModelRepository repository) : IRequestHandler<GetUsersForAdministrationQuery, (IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> {
    public Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> Handle(GetUsersForAdministrationQuery request, CancellationToken cancellationToken) {
        string? search = request.Search;
        int page = request.Page;
        int limit = request.Limit;
        UserAccountStatusFilter status = request.Status;
        return repository.GetPagedReadModelsAsync(search, page, limit, status, cancellationToken);
    }

}
