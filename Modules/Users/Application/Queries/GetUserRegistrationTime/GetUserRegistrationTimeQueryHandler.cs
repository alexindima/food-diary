using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserRegistrationTime;

namespace FoodDiary.Modules.Users.Application.Queries.GetUserRegistrationTime;

public sealed class GetUserRegistrationTimeQueryHandler(IUserLookupRepository users)
    : IRequestHandler<GetUserRegistrationTimeQuery, DateTime?> {
    public Task<DateTime?> Handle(GetUserRegistrationTimeQuery request, CancellationToken cancellationToken) =>
        users.GetRegistrationTimeAsync(request.UserId, cancellationToken);
}
