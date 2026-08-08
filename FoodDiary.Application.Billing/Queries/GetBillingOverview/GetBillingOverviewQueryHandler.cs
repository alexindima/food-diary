using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Mediator;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Queries.GetBillingOverview;

public sealed class GetBillingOverviewQueryHandler(
    IBillingOverviewReadService readService,
    ICurrentUserAccessService currentUserAccessService)
    : IRequestHandler<GetBillingOverviewQuery, Result<BillingOverviewModel>> {
    public async Task<Result<BillingOverviewModel>> Handle(
        GetBillingOverviewQuery request,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await BillingCurrentUserAccessResolver.ResolveAsync(
            request.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return BillingCurrentUserAccessResolver.ToFailure<BillingOverviewModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        return await readService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
