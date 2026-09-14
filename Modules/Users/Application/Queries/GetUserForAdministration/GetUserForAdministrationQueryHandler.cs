using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Queries.GetUserForAdministration;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Users.Queries.GetUserForAdministration;

public sealed class GetUserForAdministrationQueryHandler(IUserAdminReadModelRepository repository) : IRequestHandler<GetUserForAdministrationQuery, UserAdminReadModel?> {
    public Task<UserAdminReadModel?> Handle(GetUserForAdministrationQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        return repository.GetByIdIncludingDeletedReadModelAsync(userId, cancellationToken);
    }

}
