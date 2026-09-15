using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserBillingProfileReadModelRepository {
    Task<UserBillingProfileModel?> GetBillingProfileIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default);
}
