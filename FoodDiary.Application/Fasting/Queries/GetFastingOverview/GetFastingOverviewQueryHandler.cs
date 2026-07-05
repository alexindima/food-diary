using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetFastingOverview;

public sealed class GetFastingOverviewQueryHandler(
    IFastingReadService fastingReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetFastingOverviewQuery, Result<FastingOverviewModel>> {
    public async Task<Result<FastingOverviewModel>> Handle(GetFastingOverviewQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<FastingOverviewModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<FastingOverviewModel>(accessError);
        }

        return Result.Success(await fastingReadService.GetOverviewAsync(userId, cancellationToken).ConfigureAwait(false));
    }
}
