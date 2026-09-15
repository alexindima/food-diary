using FoodDiary.Mediator;
using FoodDiary.Modules.Fasting.Contracts.Queries.ReadCurrentFasting;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Queries.GetCurrentFasting;

public sealed class GetCurrentFastingQueryHandler(
    ISender sender,
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
        FastingSessionModel? current = await sender.Send(new ReadCurrentFastingQuery(userId), cancellationToken).ConfigureAwait(false);
        return Result.Success(current);
    }
}
