using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Queries.CheckUserAccess;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Queries.CheckUserAccess;

public sealed class CheckUserAccessQueryHandler(ICurrentUserAccessService accessService) : IRequestHandler<CheckUserAccessQuery, Error?> {
    public Task<Error?> Handle(CheckUserAccessQuery request, CancellationToken cancellationToken) =>
        accessService.EnsureCanAccessAsync(request.UserId, cancellationToken);
}
