using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserForAdministration;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Application.Queries.GetUserForAdministration;

public sealed class GetUserForAdministrationQueryHandler(IUserAdminReadModelRepository repository) : IRequestHandler<GetUserForAdministrationQuery, UserAdminReadModel?> {
    public Task<UserAdminReadModel?> Handle(GetUserForAdministrationQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        return repository.GetByIdIncludingDeletedReadModelAsync(userId, cancellationToken);
    }

}
