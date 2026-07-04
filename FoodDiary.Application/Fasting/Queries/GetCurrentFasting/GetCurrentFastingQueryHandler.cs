using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Queries.GetCurrentFasting;

public class GetCurrentFastingQueryHandler(
    IFastingOccurrenceReadRepository fastingOccurrenceRepository,
    IFastingCheckInReadRepository fastingCheckInRepository,
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

        FastingOccurrence? current = await fastingOccurrenceRepository.GetCurrentAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (current is null) {
            return Result.Success<FastingSessionModel?>(value: null);
        }

        IReadOnlyList<FastingCheckIn> checkIns = await fastingCheckInRepository.GetByOccurrenceIdsAsync([current.Id], cancellationToken).ConfigureAwait(false);
        return Result.Success<FastingSessionModel?>(current.ToModel(current.Plan, checkIns));
    }
}
