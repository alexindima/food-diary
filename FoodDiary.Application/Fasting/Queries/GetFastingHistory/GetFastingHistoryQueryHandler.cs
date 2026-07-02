using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetFastingHistory;

public class GetFastingHistoryQueryHandler(
    IFastingAnalyticsService fastingAnalyticsService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetFastingHistoryQuery, Result<PagedResponse<FastingSessionModel>>> {
    public async Task<Result<PagedResponse<FastingSessionModel>>> Handle(
        GetFastingHistoryQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<PagedResponse<FastingSessionModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<PagedResponse<FastingSessionModel>>(accessError);
        }

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
