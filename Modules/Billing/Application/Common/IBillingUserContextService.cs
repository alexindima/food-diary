using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Common;

public interface IBillingUserContextService : ICurrentUserAccessService {
    Task<Result<BillingUserProfileModel>> GetAccessibleUserProfileAsync(UserId userId, CancellationToken cancellationToken);
    Task<Result<UserBillingProfileModel>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken);
    Task<UserBillingProfileModel?> GetUserIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken);
    Task<Result<UserBillingProfileModel>> StartPremiumTrialAsync(
        UserId userId,
        DateTime startedAtUtc,
        TimeSpan duration,
        CancellationToken cancellationToken);
    Task EnsurePremiumRoleAsync(UserId userId, CancellationToken cancellationToken);
    Task RemovePremiumRoleAsync(UserId userId, CancellationToken cancellationToken);
}
