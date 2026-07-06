using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Services;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Billing.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Users;

[ExcludeFromCodeCoverage]
public sealed class UserApplicationServiceDelegationTests {
    [Fact]
    public async Task BillingUserLookupService_GetUserIncludingDeletedAsync_DelegatesToLookupRepository() {
        IUserLookupRepository repository = Substitute.For<IUserLookupRepository>();
        var service = new BillingUserLookupService(repository);
        var userId = UserId.New();
        var user = User.Create("billing@test.com", "hashed-password");
        using var cancellationTokenSource = new CancellationTokenSource();
        repository.GetByIdIncludingDeletedAsync(userId, cancellationTokenSource.Token).Returns(user);

        User? result = await service.GetUserIncludingDeletedAsync(userId, cancellationTokenSource.Token);

        Assert.Same(user, result);
        await repository.Received(1).GetByIdIncludingDeletedAsync(userId, cancellationTokenSource.Token);
    }

    [Fact]
    public async Task BillingUserLookupService_CanAccessUserAsync_ReturnsAccessPolicyResult() {
        var service = new BillingUserLookupService(Substitute.For<IUserLookupRepository>());
        var activeUser = User.Create("active@test.com", "hashed-password");
        var deletedUser = User.Create("deleted@test.com", "hashed-password");
        deletedUser.MarkDeleted(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        bool canAccessActiveUser = await service.CanAccessUserAsync(activeUser, CancellationToken.None);
        bool canAccessDeletedUser = await service.CanAccessUserAsync(deletedUser, CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(canAccessActiveUser),
            () => Assert.False(canAccessDeletedUser));
    }

    [Fact]
    public async Task AuthenticationUserLookupService_GetByTelegramUserIdAsync_DelegatesToLookupRepository() {
        IUserLookupRepository repository = Substitute.For<IUserLookupRepository>();
        var service = new AuthenticationUserLookupService(repository);
        var user = User.Create("telegram@test.com", "hashed-password");
        using var cancellationTokenSource = new CancellationTokenSource();
        repository.GetByTelegramUserIdAsync(123456789, cancellationTokenSource.Token).Returns(user);

        User? result = await service.GetByTelegramUserIdAsync(123456789, cancellationTokenSource.Token);

        Assert.Same(user, result);
        await repository.Received(1).GetByTelegramUserIdAsync(123456789, cancellationTokenSource.Token);
    }

    [Fact]
    public async Task AuthenticationUserMutationService_AddAsync_DelegatesToWriteRepository() {
        IUserLookupRepository lookupRepository = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writeRepository = Substitute.For<IUserWriteRepository>();
        var service = new AuthenticationUserMutationService(lookupRepository, writeRepository);
        var user = User.Create("add@test.com", "hashed-password");
        using var cancellationTokenSource = new CancellationTokenSource();
        writeRepository.AddAsync(user, cancellationTokenSource.Token).Returns(user);

        User result = await service.AddAsync(user, cancellationTokenSource.Token);

        Assert.Same(user, result);
        await writeRepository.Received(1).AddAsync(user, cancellationTokenSource.Token);
    }

    [Fact]
    public async Task AuthenticationUserMutationService_GetByTelegramUserIdIncludingDeletedAsync_DelegatesToLookupRepository() {
        IUserLookupRepository lookupRepository = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writeRepository = Substitute.For<IUserWriteRepository>();
        var service = new AuthenticationUserMutationService(lookupRepository, writeRepository);
        var user = User.Create("telegram-deleted@test.com", "hashed-password");
        using var cancellationTokenSource = new CancellationTokenSource();
        lookupRepository.GetByTelegramUserIdIncludingDeletedAsync(987654321, cancellationTokenSource.Token).Returns(user);

        User? result = await service.GetByTelegramUserIdIncludingDeletedAsync(987654321, cancellationTokenSource.Token);

        Assert.Same(user, result);
        await lookupRepository.Received(1).GetByTelegramUserIdIncludingDeletedAsync(987654321, cancellationTokenSource.Token);
    }

    [Fact]
    public async Task AdminUserReadService_DelegatesReadMethodsToRepositories() {
        IUserLookupRepository lookupRepository = Substitute.For<IUserLookupRepository>();
        IUserAdminReadModelRepository adminReadRepository = Substitute.For<IUserAdminReadModelRepository>();
        var service = new AdminUserReadService(lookupRepository, adminReadRepository);
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
        await lookupRepository.DidNotReceive().GetByIdIncludingDeletedAsync(userId, cancellationTokenSource.Token);
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
            user.AiConsentAcceptedAt);
}
