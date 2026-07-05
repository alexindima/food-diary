using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetCurrentFasting;

public sealed class GetCurrentFastingQueryHandler(
    IFastingReadService fastingReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetCurrentFastingQuery, Result<FastingSessionModel?>> {
    public async Task<Result<FastingSessionModel?>> Handle(
        GetCurrentFastingQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel?>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<FastingSessionModel?>(accessError);
        }

        FastingSessionModel? current = await fastingReadService.GetCurrentAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(current);
    }
}
