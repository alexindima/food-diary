using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Abstractions.Fasting.Common;

public interface IFastingCheckInWriteRepository {
    Task AddAsync(FastingCheckIn checkIn, CancellationToken cancellationToken = default);
}
