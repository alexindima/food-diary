using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserBillingService : ICurrentUserAccessService {
    Task<Result<UserBillingProfileModel>> GetAccessibleProfileAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<UserBillingProfileModel?> GetProfileIncludingDeletedAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<Result<UserBillingProfileModel>> StartPremiumTrialAsync(
        UserId userId,
        DateTime startedAtUtc,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    Task EnsurePremiumRoleAsync(UserId userId, CancellationToken cancellationToken = default);

    Task RemovePremiumRoleAsync(UserId userId, CancellationToken cancellationToken = default);
}
