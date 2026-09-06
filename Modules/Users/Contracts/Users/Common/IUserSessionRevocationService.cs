using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserSessionRevocationService {
    Task RevokeAllAsync(UserId userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default);
}
