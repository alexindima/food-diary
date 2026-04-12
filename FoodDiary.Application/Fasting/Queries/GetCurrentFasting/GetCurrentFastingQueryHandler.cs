using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetCurrentFasting;

public class GetCurrentFastingQueryHandler(
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IFastingCheckInRepository fastingCheckInRepository)
    : IQueryHandler<GetCurrentFastingQuery, Result<FastingSessionModel?>> {
    public async Task<Result<FastingSessionModel?>> Handle(
        GetCurrentFastingQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel?>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var current = await fastingOccurrenceRepository.GetCurrentAsync(userId, cancellationToken: cancellationToken);
        if (current is null) {
            return Result.Success<FastingSessionModel?>(null);
        }

        var checkIns = await fastingCheckInRepository.GetByOccurrenceIdsAsync([current.Id], cancellationToken);
        return Result.Success<FastingSessionModel?>(current.ToModel(current.Plan, checkIns));
    }
}
