using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Modules.Fasting.Application.Abstractions.Common;

public interface IFastingCheckInWriteRepository {
    Task AddAsync(FastingCheckIn checkIn, CancellationToken cancellationToken = default);
}
