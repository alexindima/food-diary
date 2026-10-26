using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Queries.AuthenticateBrowserSession;

public sealed class AuthenticateBrowserSessionQueryHandler(BrowserSessionService sessions)
    : IQueryHandler<AuthenticateBrowserSessionQuery, Result<BrowserSessionModel>> {
    public Task<Result<BrowserSessionModel>> Handle(AuthenticateBrowserSessionQuery query, CancellationToken cancellationToken) =>
        sessions.AuthenticateAsync(query.Credential, cancellationToken);
}
