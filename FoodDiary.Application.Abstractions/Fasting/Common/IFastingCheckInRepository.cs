using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Common;

public interface IFastingCheckInRepository {
    Task AddAsync(FastingCheckIn checkIn, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FastingCheckIn>> GetByOccurrenceIdsAsync(
        IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
        CancellationToken cancellationToken = default);
}
