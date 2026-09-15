using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Authentication.Queries.GetLoginEvents;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Identity.Authentication.Queries.GetLoginEvents;

public sealed class GetLoginEventsQueryHandler(IUserLoginEventQuery repository)
    : IRequestHandler<GetLoginEventsQuery, (IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)> {
    public Task<(IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)> Handle(GetLoginEventsQuery request, CancellationToken cancellationToken) =>
        repository.GetPagedAsync(request.Page, request.Limit, request.UserId, request.Search, cancellationToken, request.FromUtc, request.ToUtc, request.Provider, request.Device);
}
