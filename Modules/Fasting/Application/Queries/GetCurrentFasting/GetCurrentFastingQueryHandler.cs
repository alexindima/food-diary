using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Queries.GetCurrentFasting;

public sealed class GetCurrentFastingQueryHandler(
    IFastingReadService fastingReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetCurrentFastingQuery, Result<FastingSessionModel?>> {
    public async Task<Result<FastingSessionModel?>> Handle(
        GetCurrentFastingQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<FastingSessionModel?>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        FastingSessionModel? current = await fastingReadService.GetCurrentAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(current);
    }
}
