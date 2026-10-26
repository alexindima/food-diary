using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Queries.GetBrowserSessionProfile;

public sealed class GetBrowserSessionProfileQueryHandler(IUserAuthenticationIdentityService users, TimeProvider timeProvider)
    : IQueryHandler<GetBrowserSessionProfileQuery, Result<UserAuthenticationPrincipalModel>> {
    public Task<Result<UserAuthenticationPrincipalModel>> Handle(GetBrowserSessionProfileQuery query, CancellationToken cancellationToken) =>
        users.GetAuthenticationPrincipalAsync(query.UserId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
}
