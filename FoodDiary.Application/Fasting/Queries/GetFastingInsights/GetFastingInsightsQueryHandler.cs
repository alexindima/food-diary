using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetFastingInsights;

public sealed class GetFastingInsightsQueryHandler(
    IFastingReadService fastingReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetFastingInsightsQuery, Result<FastingInsightsModel>> {
    public async Task<Result<FastingInsightsModel>> Handle(
        GetFastingInsightsQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<FastingInsightsModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<FastingInsightsModel>(accessError);
        }

        return Result.Success(await fastingReadService.GetInsightsAsync(userId, cancellationToken).ConfigureAwait(false));
    }
}
