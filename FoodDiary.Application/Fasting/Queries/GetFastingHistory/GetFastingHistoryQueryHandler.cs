using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Queries.GetFastingHistory;

public class GetFastingHistoryQueryHandler(
    IFastingSessionRepository fastingRepository)
    : IQueryHandler<GetFastingHistoryQuery, Result<IReadOnlyList<FastingSessionModel>>> {
    public async Task<Result<IReadOnlyList<FastingSessionModel>>> Handle(
        GetFastingHistoryQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<FastingSessionModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var sessions = await fastingRepository.GetHistoryAsync(userId, query.From, query.To, cancellationToken);
        var models = sessions.Select(s => s.ToModel()).ToList();
        return Result.Success<IReadOnlyList<FastingSessionModel>>(models);
    }
}
