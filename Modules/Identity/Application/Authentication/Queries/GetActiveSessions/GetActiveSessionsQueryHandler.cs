using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Services.UserAgents;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Validation;

namespace FoodDiary.Modules.Identity.Application.Authentication.Queries.GetActiveSessions;

public sealed class GetActiveSessionsQueryHandler(IRefreshTokenSessionReadModelRepository repository)
    : IQueryHandler<GetActiveSessionsQuery, Result<IReadOnlyList<ActiveSessionModel>>> {
    public async Task<Result<IReadOnlyList<ActiveSessionModel>>> Handle(
        GetActiveSessionsQuery query,
        CancellationToken cancellationToken) {
        UserId userId = query.UserId;
        if (!await repository.IsActiveAsync(userId, query.CurrentSessionId, cancellationToken).ConfigureAwait(false)) {
            return Result.Failure<IReadOnlyList<ActiveSessionModel>>(AuthenticationErrors.InvalidToken);
        }

        int page = PaginationPolicy.NormalizePage(query.Page);
        int limit = PaginationPolicy.NormalizePageSize(query.Limit);
        IReadOnlyList<RefreshTokenSessionReadModel> sessions = await repository
            .GetActivePageReadModelsAsync(userId, page, limit, cancellationToken)
            .ConfigureAwait(false);

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
