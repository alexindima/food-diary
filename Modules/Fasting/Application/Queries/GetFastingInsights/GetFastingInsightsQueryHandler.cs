using FoodDiary.Mediator;
using FoodDiary.Modules.Fasting.Contracts.Queries.ReadFastingInsights;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Queries.GetFastingInsights;

public sealed class GetFastingInsightsQueryHandler(
    ISender sender,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetFastingInsightsQuery, Result<FastingInsightsModel>> {
    public async Task<Result<FastingInsightsModel>> Handle(
        GetFastingInsightsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<FastingInsightsModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        return Result.Success(await sender.Send(new ReadFastingInsightsQuery(userId), cancellationToken).ConfigureAwait(false));
    }
}
