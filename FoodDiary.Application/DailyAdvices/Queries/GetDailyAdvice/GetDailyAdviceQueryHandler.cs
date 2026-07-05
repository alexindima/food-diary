using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.DailyAdvices.Common;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;

public sealed class GetDailyAdviceQueryHandler(
    IDailyAdviceReadService adviceReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetDailyAdviceQuery, Result<DailyAdviceModel>> {
    public async Task<Result<DailyAdviceModel>> Handle(GetDailyAdviceQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<DailyAdviceModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<DailyAdviceModel>(accessError);
        }

        return await adviceReadService.GetForDateAsync(query.Date, query.Locale, cancellationToken).ConfigureAwait(false);
    }
}
