using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserAdminReadRepository {
    async Task<UserAdminReadModel?> GetByIdIncludingDeletedReadModelAsync(
        UserId id,
        CancellationToken cancellationToken = default) {
        if (this is not IUserLookupRepository lookupRepository) {
            throw new NotSupportedException("Admin user read model lookup requires an IUserLookupRepository implementation.");
        }

        User? user = await lookupRepository.GetByIdIncludingDeletedAsync(id, cancellationToken).ConfigureAwait(false);
        return user is null ? null : ToReadModel(user);
    }

    Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
        string? search,
        int page,
        int limit,
        bool includeDeleted,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
        string? search,
        int page,
        int limit,
        UserAccountStatusFilter status,
        CancellationToken cancellationToken = default) =>
        GetPagedAsync(search, page, limit, status is UserAccountStatusFilter.All or UserAccountStatusFilter.Deleted, cancellationToken);

    async Task<(IReadOnlyList<UserAdminReadModel> Items, int TotalItems)> GetPagedReadModelsAsync(
        string? search,
        int page,
        int limit,
        UserAccountStatusFilter status,
        CancellationToken cancellationToken = default) {
        (IReadOnlyList<User> Items, int TotalItems) = await GetPagedAsync(
            search,
            page,
            limit,
            status,
            cancellationToken).ConfigureAwait(false);

        return ([.. Items.Select(ToReadModel)], TotalItems);
    }

    Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)>
        GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default);

    async Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<UserAdminReadModel> RecentUsers)>
        GetAdminDashboardSummaryReadModelsAsync(int recentLimit, CancellationToken cancellationToken = default) {
        (int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers) =
            await GetAdminDashboardSummaryAsync(recentLimit, cancellationToken).ConfigureAwait(false);

        return (TotalUsers, ActiveUsers, PremiumUsers, DeletedUsers, [.. RecentUsers.Select(ToReadModel)]);
    }

    private static UserAdminReadModel ToReadModel(User user) =>
        new(
            user.Id.Value,
            user.Email,
            user.HasPassword,
            user.Username,
            user.FirstName,
            user.LastName,
            user.BirthDate,
            user.Gender,
            user.Weight,
            user.DesiredWeight,
            user.DesiredWaist,
            user.Height,
            user.ActivityLevel.ToString(),
            user.DailyCalorieTarget,
            user.ProteinTarget,
            user.FatTarget,
            user.CarbTarget,
            user.FiberTarget,
            user.StepGoal,
            user.WaterGoal,
            user.HydrationGoal,
            user.CalorieCyclingEnabled,
            user.MondayCalories,
            user.TuesdayCalories,
            user.WednesdayCalories,
            user.ThursdayCalories,
            user.FridayCalories,
            user.SaturdayCalories,
            user.SundayCalories,
            user.ProfileImage,
            user.ProfileImageAssetId?.Value,
            user.DashboardLayoutJson,
            user.Language,
            user.Theme,
            user.UiStyle,
            user.PushNotificationsEnabled,
            user.FastingPushNotificationsEnabled,
            user.SocialPushNotificationsEnabled,
            user.FastingCheckInReminderHours,
            user.FastingCheckInFollowUpReminderHours,
            user.TelegramUserId,
            user.IsActive,
            user.IsEmailConfirmed,
            user.CreatedOnUtc,
            user.DeletedAt,
            user.LastLoginAtUtc,
            [.. user.GetRoleNames()],
            user.AiInputTokenLimit,
            user.AiOutputTokenLimit,
            user.AiConsentAcceptedAt);
}
