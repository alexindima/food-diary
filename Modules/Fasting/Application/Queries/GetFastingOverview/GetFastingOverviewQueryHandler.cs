using FoodDiary.Mediator;
using FoodDiary.Modules.Fasting.Contracts.Queries.ReadFastingOverview;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Queries.GetFastingOverview;

public sealed class GetFastingOverviewQueryHandler(
    ISender sender,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetFastingOverviewQuery, Result<FastingOverviewModel>> {
    public async Task<Result<FastingOverviewModel>> Handle(GetFastingOverviewQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<FastingOverviewModel>(userIdResult);
        }

        return Result.Success(await sender.Send(new ReadFastingOverviewQuery(userIdResult.Value), cancellationToken).ConfigureAwait(false));
    }
}
