using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserAdministrationReadRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task GetPagedReadModelsAsync_PreservesStatusOrderingPagingAndDetachedRoles() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var oldest = User.Create("oldest@example.com", "hash");
        var inactive = User.Create("inactive@example.com", "hash");
        var newest = User.Create("newest@example.com", "hash");
        var deleted = User.Create("deleted@example.com", "hash");
        inactive.Deactivate();
        deleted.MarkDeleted(DateTime.UtcNow);
        User[] ordered = [oldest, inactive, newest, deleted];
        context.Users.AddRange(ordered);
        DateTime start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (int index = 0; index < ordered.Length; index++) {
            context.Entry(ordered[index]).Property(user => user.CreatedOnUtc).CurrentValue = start.AddDays(index);
        }

        Role premium = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        context.UserRoles.Add(new UserRole(newest.Id, premium.Id));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reader = new UserAdministrationReadRepository(context);

        (IReadOnlyList<UserAdminReadModel> first, int total) = await reader.GetPagedReadModelsAsync(search: null, page: 1, limit: 1, UserAccountStatusFilter.Active);
        UserAdminReadModel firstItem = Assert.Single(first);
        Assert.Multiple(
            () => Assert.Equal(2, total),
            () => Assert.Equal(newest.Id.Value, firstItem.Id),
            () => Assert.Equal([RoleNames.Premium], firstItem.Roles));
        Assert.Equal(oldest.Id.Value, Assert.Single((await reader.GetPagedReadModelsAsync(search: null, page: 2, limit: 1, UserAccountStatusFilter.Active)).Items).Id);
        Assert.Equal(inactive.Id.Value, Assert.Single((await reader.GetPagedReadModelsAsync(search: null, page: 1, limit: 10, UserAccountStatusFilter.Inactive)).Items).Id);
        Assert.Equal(deleted.Id.Value, Assert.Single((await reader.GetPagedReadModelsAsync(search: null, page: 1, limit: 10, UserAccountStatusFilter.Deleted)).Items).Id);
        Assert.Equal(new[] { deleted.Id.Value, newest.Id.Value, inactive.Id.Value, oldest.Id.Value },
            (await reader.GetPagedReadModelsAsync(search: null, page: 1, limit: 10, UserAccountStatusFilter.All)).Items.Select(user => user.Id));
        Assert.Equal(4, (await reader.GetPagedReadModelsAsync(search: null, page: 1, limit: 10, (UserAccountStatusFilter)999)).TotalItems);
        Assert.Equal(2, (await reader.GetPagedAsync(search: null, page: 1, limit: 10, includeDeleted: false)).TotalItems);
        Assert.Equal(4, (await reader.GetPagedAsync(search: null, page: 1, limit: 10, includeDeleted: true)).TotalItems);
        (IReadOnlyList<UserAdminReadModel> beyondEnd, int beyondTotal) = await reader.GetPagedReadModelsAsync(search: null, page: 20, limit: 10, UserAccountStatusFilter.All);
        Assert.Multiple(
            () => Assert.Empty(beyondEnd),
            () => Assert.Equal(4, beyondTotal),
            () => Assert.Empty(context.ChangeTracker.Entries()));
    }

    [RequiresDockerFact]
    public async Task GetPagedReadModelsAsync_EscapesPercentUnderscoreAndBackslashInProfileSearch() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var literal = User.Create("literal@example.com", "hash");
        literal.UpdatePersonalInfo(firstName: @"Mix_%\Value");
        var wildcardLookalike = User.Create("other@example.com", "hash");
        wildcardLookalike.UpdatePersonalInfo(firstName: "MixXXValue");
        context.Users.AddRange(literal, wildcardLookalike);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reader = new UserAdministrationReadRepository(context);

        foreach (string term in new[] { "_", "%", @"\", @"  mix_%\value  " }) {
            (IReadOnlyList<UserAdminReadModel> items, int total) = await reader.GetPagedReadModelsAsync(search: term, page: 1, limit: 10, UserAccountStatusFilter.All);
            Assert.Equal(literal.Id.Value, Assert.Single(items).Id);
            Assert.Equal(1, total);
        }

        Assert.Empty(context.ChangeTracker.Entries());
    }

    [RequiresDockerFact]
    public async Task GetByIdIncludingDeletedReadModelAsync_UsesPersistedProfileAndRolesWithoutGoalHistory() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("profile@example.com", "hash", hasPassword: false);
        user.UpdatePersonalInfo(username: "profile-user", firstName: "Saved", lastName: "Profile", birthDate: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc), gender: "M", weight: 80, height: 180);
        user.UpdatePreferences(new UserPreferenceUpdate(Language: "ru", Theme: "dark"));
        user.LinkTelegram(456789);
        user.StartWeightGoal(70, 80, DateTime.UtcNow);
        user.StartWaistGoal(80, 90, DateTime.UtcNow);
        var deleted = User.Create("deleted-profile@example.com", "hash");
        deleted.MarkDeleted(DateTime.UtcNow);
        context.Users.AddRange(user, deleted);
        Role premium = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        Role support = await context.Roles.SingleAsync(role => role.Name == RoleNames.Support);
        context.UserRoles.AddRange(new UserRole(user.Id, premium.Id), new UserRole(user.Id, support.Id));
        await context.SaveChangesAsync();
        user.UpdatePersonalInfo(firstName: "Unsaved");
        var reader = new UserAdministrationReadRepository(context);

        UserAdminReadModel? model = await reader.GetByIdIncludingDeletedReadModelAsync(user.Id);
        Assert.NotNull(model);
        Assert.Multiple(
            () => Assert.Equal(user.Id.Value, model.Id),
            () => Assert.Equal(user.Email, model.Email),
            () => Assert.False(model.HasPassword),
            () => Assert.Equal("Saved", model.FirstName),
            () => Assert.Equal("Profile", model.LastName),
            () => Assert.Equal("profile-user", model.Username),
            () => Assert.Equal(user.BirthDate, model.BirthDate),
            () => Assert.Equal(user.Gender, model.Gender),
            () => Assert.Equal(user.WeightKg, model.WeightKg),
            () => Assert.Equal(user.HeightCm, model.HeightCm),
            () => Assert.Equal(user.DesiredWeightKg, model.DesiredWeightKg),
            () => Assert.Equal(user.DesiredWaistCm, model.DesiredWaistCm),
            () => Assert.Equal(user.ActivityLevel.ToString(), model.ActivityLevel),
            () => Assert.Equal("ru", model.Language),
            () => Assert.Equal("dark", model.Theme),
            () => Assert.Equal(user.TelegramUserId, model.TelegramUserId),
            () => Assert.Equal(user.AiInputTokenLimit, model.AiInputTokenLimit),
            () => Assert.Equal(user.AiOutputTokenLimit, model.AiOutputTokenLimit),
            () => Assert.Equal(new[] { RoleNames.Premium, RoleNames.Support }.Order(StringComparer.Ordinal), model.Roles.Order(StringComparer.Ordinal)));
        Assert.Equal("Unsaved", user.FirstName);
        Assert.Same(user, context.Entry(user).Entity);
        Assert.Null(await reader.GetByIdIncludingDeletedReadModelAsync(UserId.New()));
        UserAdminReadModel? deletedModel = await reader.GetByIdIncludingDeletedReadModelAsync(deleted.Id);
        Assert.NotNull(deletedModel);
        Assert.NotNull(deletedModel.DeletedAt);

        context.ChangeTracker.Clear();
        User detached = Assert.Single((await reader.GetPagedAsync(search: user.Email, page: 1, limit: 10, includeDeleted: false)).Items);
        Assert.Multiple(
            () => Assert.Empty(detached.WeightGoals),
            () => Assert.Empty(detached.WaistGoals),
            () => Assert.Equal(2, detached.UserRoles.Count),
            () => Assert.Empty(context.ChangeTracker.Entries()));
    }

    [RequiresDockerFact]
    public async Task GetAdminDashboardSummaryReadModelsAsync_PreservesPremiumAndRecentUserSemantics() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var active = User.Create("active-summary@example.com", "hash");
        var inactivePremium = User.Create("inactive-summary@example.com", "hash");
        var deletedPremium = User.Create("deleted-summary@example.com", "hash");
        inactivePremium.Deactivate();
        deletedPremium.MarkDeleted(DateTime.UtcNow);
        context.Users.AddRange(active, inactivePremium, deletedPremium);
        DateTime start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        context.Entry(active).Property(user => user.CreatedOnUtc).CurrentValue = start;
        context.Entry(inactivePremium).Property(user => user.CreatedOnUtc).CurrentValue = start.AddDays(1);
        context.Entry(deletedPremium).Property(user => user.CreatedOnUtc).CurrentValue = start.AddDays(2);
        Role premium = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        context.UserRoles.AddRange(new UserRole(inactivePremium.Id, premium.Id), new UserRole(deletedPremium.Id, premium.Id));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reader = new UserAdministrationReadRepository(context);

        (int totalUsers, int activeUsers, int premiumUsers, int deletedUsers, IReadOnlyList<UserAdminReadModel> recentUsers) = await reader.GetAdminDashboardSummaryReadModelsAsync(10);
        Assert.Multiple(
            () => Assert.Equal(3, totalUsers),
            () => Assert.Equal(1, activeUsers),
            () => Assert.Equal(2, premiumUsers),
            () => Assert.Equal(1, deletedUsers),
            () => Assert.Equal(new[] { inactivePremium.Id.Value, active.Id.Value }, recentUsers.Select(user => user.Id)),
            () => Assert.Empty(context.ChangeTracker.Entries()));
        Assert.Equal(inactivePremium.Id.Value, Assert.Single((await reader.GetAdminDashboardSummaryReadModelsAsync(1)).RecentUsers).Id);
        (int noRecentTotal, int noRecentActive, int noRecentPremium, int noRecentDeleted, IReadOnlyList<UserAdminReadModel> noRecentUsers) = await reader.GetAdminDashboardSummaryReadModelsAsync(0);
        Assert.Multiple(
            () => Assert.Empty(noRecentUsers),
            () => Assert.Equal(3, noRecentTotal),
            () => Assert.Equal(1, noRecentActive),
            () => Assert.Equal(2, noRecentPremium),
            () => Assert.Equal(1, noRecentDeleted));
    }

    [RequiresDockerFact]
    public async Task EmptyReads_ReturnZeroAndPropagateCancellationAcrossBothPorts() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var reader = new UserAdministrationReadRepository(context);
        (int totalUsers, int activeUsers, int premiumUsers, int deletedUsers, IReadOnlyList<UserAdminReadModel> recentUsers) = await reader.GetAdminDashboardSummaryReadModelsAsync(10);
        Assert.Multiple(
            () => Assert.Equal(0, totalUsers),
            () => Assert.Equal(0, activeUsers),
            () => Assert.Equal(0, premiumUsers),
            () => Assert.Equal(0, deletedUsers),
            () => Assert.Empty(recentUsers));
        Assert.Empty((await reader.GetPagedReadModelsAsync(search: null, page: 1, limit: 10, UserAccountStatusFilter.All)).Items);
        Assert.Null(await reader.GetByIdIncludingDeletedReadModelAsync(UserId.New()));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        CancellationToken token = cancellation.Token;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.GetByIdIncludingDeletedReadModelAsync(UserId.New(), token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.GetPagedAsync(search: null, page: 1, limit: 10, includeDeleted: false, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.GetPagedAsync(search: null, page: 1, limit: 10, UserAccountStatusFilter.All, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.GetPagedReadModelsAsync(search: null, page: 1, limit: 10, UserAccountStatusFilter.All, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.GetAdminDashboardSummaryAsync(10, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.GetAdminDashboardSummaryReadModelsAsync(10, token));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_NormalizesPagingAndEscapesLikePattern() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var matchingUser = User.Create("100%real@example.com", "hash");
        matchingUser.UpdatePersonalInfo(new FoodDiary.Domain.ValueObjects.UserPersonalInfoUpdate(Username: "special_user"));
        var otherUser = User.Create("1000real@example.com", "hash");
        otherUser.UpdatePersonalInfo(new FoodDiary.Domain.ValueObjects.UserPersonalInfoUpdate(Username: "plain_user"));
        context.Users.AddRange(matchingUser, otherUser);
        await context.SaveChangesAsync();

        var repository = new UserAdministrationReadRepository(context);

        (IReadOnlyList<User>? items, int totalItems) = await repository.GetPagedAsync(
            search: "100%real",
            page: 0,
            limit: 0,
            includeDeleted: false);

        User item = Assert.Single(items);
        Assert.Equal(1, totalItems);
        Assert.Equal(matchingUser.Id, item.Id);
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_ReturnsPagedUsersWithRolesLoaded() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        Role premiumRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        var user = User.Create($"paged-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.UserRoles.Add(new UserRole(user.Id, premiumRole.Id));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new UserAdministrationReadRepository(context);

        (IReadOnlyList<User>? items, int totalItems) = await repository.GetPagedAsync(
            search: user.Email,
            page: 1,
            limit: 10,
            includeDeleted: false);

        User item = Assert.Single(items);
        Assert.Equal(1, totalItems);
        Assert.Single(item.UserRoles);
        Assert.Equal(RoleNames.Premium, item.UserRoles.Single().Role.Name);
        Assert.Equal(EntityState.Detached, context.Entry(item).State);
    }

    [RequiresDockerFact]
    public async Task GetAdminDashboardSummaryAsync_CountsPremiumAndSkipsDeletedInRecentUsers() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        Role premiumRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        var firstUser = User.Create("first@example.com", "hash");
        var premiumUser = User.Create("premium@example.com", "hash");
        var deletedUser = User.Create("deleted@example.com", "hash");
        deletedUser.MarkDeleted(DateTime.UtcNow);

        context.Users.AddRange(firstUser, premiumUser, deletedUser);
        context.UserRoles.Add(new UserRole(premiumUser.Id, premiumRole.Id));
        await context.SaveChangesAsync();

        var repository = new UserAdministrationReadRepository(context);

        (int totalUsers, int activeUsers, int premiumUsers, int deletedUsers, IReadOnlyList<User> recentUsers) = await repository.GetAdminDashboardSummaryAsync(recentLimit: 10);

        Assert.Equal(3, totalUsers);
        Assert.Equal(2, activeUsers);
        Assert.Equal(1, premiumUsers);
        Assert.Equal(1, deletedUsers);
        Assert.Equal(2, recentUsers.Count);
        Assert.DoesNotContain(recentUsers, user => user.Id == deletedUser.Id);
    }

}
