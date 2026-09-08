using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminRetention;

public sealed class GetAdminRetentionQueryHandler(IAdminRetentionReader reader, TimeProvider timeProvider)
    : IQueryHandler<GetAdminRetentionQuery, Result<AdminRetentionReport>> {
    public async Task<Result<AdminRetentionReport>> Handle(GetAdminRetentionQuery query, CancellationToken cancellationToken) {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        DateOnly from = query.From ?? today.AddDays(-29);
        DateOnly to = query.To ?? today;
        if (from > to || to > today || from < DateOnly.FromDateTime(DateTime.UnixEpoch)) {
            return Result.Failure<AdminRetentionReport>(Errors.Validation.Invalid("Period", "Invalid cohort date range."));
        }
        return Result.Success(await reader.GetAsync(from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), now, cancellationToken).ConfigureAwait(false));
    }
}
