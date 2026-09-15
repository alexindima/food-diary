using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Queries.CheckUserAccess;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Queries.CheckUserAccess;

public sealed class CheckUserAccessQueryHandler(ICurrentUserAccessService accessService) : IRequestHandler<CheckUserAccessQuery, Error?> {
    public Task<Error?> Handle(CheckUserAccessQuery request, CancellationToken cancellationToken) =>
        accessService.EnsureCanAccessAsync(request.UserId, cancellationToken);
}
