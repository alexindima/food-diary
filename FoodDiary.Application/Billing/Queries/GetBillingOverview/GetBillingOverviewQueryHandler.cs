using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Queries.GetBillingOverview;

public sealed class GetBillingOverviewQueryHandler(
    IBillingOverviewReadService readService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetBillingOverviewQuery, Result<BillingOverviewModel>> {
    public async Task<Result<BillingOverviewModel>> Handle(
        GetBillingOverviewQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<BillingOverviewModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        return await readService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
