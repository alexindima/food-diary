using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Application.Abstractions.Common;

public interface IUserBillingProfileReadModelRepository {
    Task<UserBillingProfileModel?> GetBillingProfileIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default);
}
