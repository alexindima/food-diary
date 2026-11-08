using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Application.Abstractions.Common;

public interface IUserActivityRepository {
    Task RecordAsync(UserId userId, DateTime occurredAtUtc, CancellationToken cancellationToken);
}
