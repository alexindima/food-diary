using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Contracts.Authentication.Models;
using FoodDiary.Modules.Identity.Contracts.Authentication.Queries.GetLoginEvents;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Identity.Application.Authentication.Queries.GetLoginEvents;

public sealed class GetLoginEventsQueryHandler(IUserLoginEventQuery repository)
    : IRequestHandler<GetLoginEventsQuery, (IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)> {
    public Task<(IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)> Handle(GetLoginEventsQuery request, CancellationToken cancellationToken) =>
        repository.GetPagedAsync(request.Page, request.Limit, request.UserId, request.Search, cancellationToken, request.FromUtc, request.ToUtc, request.Provider, request.Device);
}
