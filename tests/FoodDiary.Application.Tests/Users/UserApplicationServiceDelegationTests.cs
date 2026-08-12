using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Services;
using FoodDiary.Application.Users.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Users;

[ExcludeFromCodeCoverage]
public sealed class UserApplicationServiceDelegationTests {
    [Fact]
    public async Task UserIdentityMutationService_EnsureRolesByNamesAsync_DelegatesToRoleCatalog() {
        IUserRoleCatalogService roleCatalog = Substitute.For<IUserRoleCatalogService>();
        IReadOnlyList<string> names = ["Admin"];
        IReadOnlyList<Role> roles = [Role.Create("Admin")];
        roleCatalog.EnsureRolesByNamesAsync(names, Arg.Any<CancellationToken>()).Returns(roles);
        var service = new UserIdentityMutationService(Substitute.For<IUserWriteRepository>(), roleCatalog);

        IReadOnlyList<Role> result = await service.EnsureRolesByNamesAsync(names, CancellationToken.None);

        Assert.Same(roles, result);
        await roleCatalog.Received(1).EnsureRolesByNamesAsync(names, CancellationToken.None);
    }

    [Fact]
    public async Task UserIdentityMutationService_AddAndUpdate_DelegateToWriteRepository() {
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        var user = User.Create("identity-mutation@example.com", "hash");
        writer.AddAsync(user, Arg.Any<CancellationToken>()).Returns(user);
        var service = new UserIdentityMutationService(writer, Substitute.For<IUserRoleCatalogService>());

        User added = await service.AddAsync(user, CancellationToken.None);
        await service.UpdateAsync(user, CancellationToken.None);

        Assert.Same(user, added);
        await writer.Received(1).UpdateAsync(user, CancellationToken.None);
    }

    [Fact]
    public async Task AdminUserReadService_DelegatesReadMethodsToRepositories() {
        IUserAdminReadModelRepository adminReadRepository = Substitute.For<IUserAdminReadModelRepository>();
        var service = new AdminUserReadService(new UserAdministrationReadService(adminReadRepository));
        var userId = UserId.New();
        var user = User.Create("admin@test.com", "hashed-password");
        UserAdminReadModel userReadModel = ToAdminReadModel(user);
        IReadOnlyList<UserAdminReadModel> users = [userReadModel];
        using var cancellationTokenSource = new CancellationTokenSource();
        adminReadRepository.GetByIdIncludingDeletedReadModelAsync(userId, cancellationTokenSource.Token).Returns(userReadModel);
        adminReadRepository
            .GetPagedReadModelsAsync("adm", page: 2, limit: 5, UserAccountStatusFilter.Deleted, cancellationTokenSource.Token)
            .Returns((users, 10));
        adminReadRepository
            .GetAdminDashboardSummaryReadModelsAsync(recentLimit: 3, cancellationTokenSource.Token)
            .Returns((TotalUsers: 10, ActiveUsers: 8, PremiumUsers: 2, DeletedUsers: 1, RecentUsers: users));

        AdminUserModel? byId = await service.GetByIdIncludingDeletedAsync(userId, cancellationTokenSource.Token);
        (IReadOnlyList<AdminUserModel> items, int totalItems) = await service.GetPagedAsync("adm", 2, 5, UserAccountStatusFilter.Deleted, cancellationTokenSource.Token);
        AdminDashboardSummaryModel summary = await service.GetDashboardSummaryAsync(
            recentLimit: 3,
            pendingReportsCount: 4,
            cancellationTokenSource.Token);

        Assert.Multiple(
            () => Assert.Equal(user.Id.Value, byId?.Id),
            () => Assert.Equal(user.Id.Value, Assert.Single(items).Id),
            () => Assert.Equal(10, totalItems),
            () => Assert.Equal(10, summary.TotalUsers),
            () => Assert.Equal(8, summary.ActiveUsers),
            () => Assert.Equal(2, summary.PremiumUsers),
            () => Assert.Equal(1, summary.DeletedUsers),
            () => Assert.Equal(4, summary.PendingReportsCount),
            () => Assert.Equal(user.Id.Value, Assert.Single(summary.RecentUsers).Id));
        await adminReadRepository.Received(1).GetByIdIncludingDeletedReadModelAsync(userId, cancellationTokenSource.Token);
        await adminReadRepository.Received(1).GetPagedReadModelsAsync("adm", 2, 5, UserAccountStatusFilter.Deleted, cancellationTokenSource.Token);
        await adminReadRepository.Received(1).GetAdminDashboardSummaryReadModelsAsync(3, cancellationTokenSource.Token);
    }

    private static UserAdminReadModel ToAdminReadModel(User user) =>
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
            user.AiConsentAcceptedAt,
            user.MustChangePassword);
}
