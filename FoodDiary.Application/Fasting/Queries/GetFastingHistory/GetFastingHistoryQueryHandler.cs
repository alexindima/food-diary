using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Queries.GetFastingHistory;

public class GetFastingHistoryQueryHandler(
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IFastingCheckInRepository fastingCheckInRepository)
    : IQueryHandler<GetFastingHistoryQuery, Result<PagedResponse<FastingSessionModel>>> {
    public async Task<Result<PagedResponse<FastingSessionModel>>> Handle(
        GetFastingHistoryQuery query, CancellationToken cancellationToken) {
        var userId = new UserId(query.UserId!.Value);
        var (occurrences, totalItems) = await fastingOccurrenceRepository.GetPagedByUserAsync(
            userId,
            from: query.From,
            to: query.To,
            page: query.Page,
            limit: query.Limit,
            cancellationToken: cancellationToken);
        var occurrenceIds = occurrences.Select(static occurrence => occurrence.Id).ToArray();
        var checkIns = await fastingCheckInRepository.GetByOccurrenceIdsAsync(occurrenceIds, cancellationToken);
        var checkInsByOccurrence = checkIns
            .GroupBy(static checkIn => checkIn.OccurrenceId)
            .ToDictionary(static group => group.Key, static group => (IReadOnlyList<FastingCheckIn>)group.ToList());
        var models = occurrences
            .Select(occurrence => occurrence.ToModel(
                occurrence.Plan,
                checkInsByOccurrence.GetValueOrDefault(occurrence.Id)))
            .ToList();
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)query.Limit);
        return Result.Success(new PagedResponse<FastingSessionModel>(models, query.Page, query.Limit, totalPages, totalItems));
    }
}
