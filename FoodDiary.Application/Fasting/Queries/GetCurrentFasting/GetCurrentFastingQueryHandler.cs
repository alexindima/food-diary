using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetCurrentFasting;

public class GetCurrentFastingQueryHandler(
    IFastingSessionRepository fastingRepository)
    : IQueryHandler<GetCurrentFastingQuery, Result<FastingSessionModel?>> {
    public async Task<Result<FastingSessionModel?>> Handle(
        GetCurrentFastingQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<FastingSessionModel?>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var current = await fastingRepository.GetCurrentAsync(userId, cancellationToken);
        return Result.Success(current?.ToModel());
    }
}
