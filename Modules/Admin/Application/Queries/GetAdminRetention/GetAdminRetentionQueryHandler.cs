using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminRetention;

public sealed class GetAdminRetentionQueryHandler(IAdminRetentionReader reader, TimeProvider timeProvider)
    : IQueryHandler<GetAdminRetentionQuery, Result<AdminRetentionReport>> {
    public async Task<Result<AdminRetentionReport>> Handle(GetAdminRetentionQuery query, CancellationToken cancellationToken) {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        DateOnly from = query.From ?? today.AddDays(-29);
        DateOnly to = query.To ?? today;
        bool hasCohortPeriod = query.CohortFrom.HasValue || query.CohortTo.HasValue;
        DateOnly cohortFrom = query.CohortFrom ?? from;
        DateOnly cohortTo = query.CohortTo ?? (hasCohortPeriod ? today : to);
        if (cohortFrom > cohortTo || cohortTo > today || cohortFrom < DateOnly.FromDateTime(DateTime.UnixEpoch) || from > to || to > today || from < DateOnly.FromDateTime(DateTime.UnixEpoch)) {
            return Result.Failure<AdminRetentionReport>(Errors.Validation.Invalid("Period", "Invalid cohort date range."));
        }
        return Result.Success(await reader.GetAsync(from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), now, cancellationToken,
            hasCohortPeriod ? cohortFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null,
            hasCohortPeriod ? cohortTo.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null).ConfigureAwait(false));
    }
}
