using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Application.Identity.Authentication.Services.UserAgents;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Queries.GetActiveSessions;

public sealed class GetActiveSessionsQueryHandler(IRefreshTokenSessionReadRepository repository)
    : IQueryHandler<GetActiveSessionsQuery, Result<IReadOnlyList<ActiveSessionModel>>> {
    public async Task<Result<IReadOnlyList<ActiveSessionModel>>> Handle(
        GetActiveSessionsQuery query,
        CancellationToken cancellationToken) {
        IReadOnlyList<UserRefreshTokenSession> sessions = await repository
            .GetActiveByUserIdAsync(new UserId(query.UserId), cancellationToken)
            .ConfigureAwait(false);
        if (!sessions.Any(session => session.Id == query.CurrentSessionId)) {
            return Result.Failure<IReadOnlyList<ActiveSessionModel>>(Errors.Authentication.InvalidToken);
        }

        ActiveSessionModel[] models = [.. sessions.Select(session => {
            ParsedUserAgent userAgent = UserAgentParser.Parse(session.UserAgent);
            return new ActiveSessionModel(
                session.Id,
                session.Id == query.CurrentSessionId,
                session.AuthProvider,
                userAgent.BrowserName,
                userAgent.OperatingSystem,
                userAgent.DeviceType,
                session.CreatedAtUtc,
                session.LastRotatedAtUtc);
        })];
        return Result.Success<IReadOnlyList<ActiveSessionModel>>(models);
    }
}
