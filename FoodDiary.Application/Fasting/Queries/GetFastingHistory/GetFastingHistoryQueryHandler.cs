using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetFastingHistory;

public sealed class GetFastingHistoryQueryHandler(
    IFastingAnalyticsService fastingAnalyticsService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetFastingHistoryQuery, Result<PagedResponse<FastingSessionModel>>> {
    public async Task<Result<PagedResponse<FastingSessionModel>>> Handle(
        GetFastingHistoryQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<PagedResponse<FastingSessionModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        return Result.Success(await fastingAnalyticsService.GetHistoryAsync(
            userId,
            query.Page,
            query.Limit,
            NormalizeUtc(query.From),
            NormalizeUtc(query.To),
            cancellationToken).ConfigureAwait(false));
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
}
