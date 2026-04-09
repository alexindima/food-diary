using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetFastingHistory;

public class GetFastingHistoryQueryHandler(
    IFastingOccurrenceRepository fastingOccurrenceRepository)
    : IQueryHandler<GetFastingHistoryQuery, Result<IReadOnlyList<FastingSessionModel>>> {
    public async Task<Result<IReadOnlyList<FastingSessionModel>>> Handle(
        GetFastingHistoryQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<FastingSessionModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var occurrences = await fastingOccurrenceRepository.GetByUserAsync(
            userId,
            from: query.From,
            to: query.To,
            cancellationToken: cancellationToken);
        var models = occurrences.Select(static occurrence => occurrence.ToModel()).ToList();
        return Result.Success<IReadOnlyList<FastingSessionModel>>(models);
    }
}
