using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SessionPrincipalProjectionIntegrationTests(PostgresDatabaseFixture fixture) {
    [RequiresDockerFact]
    public async Task Principal_PreservesTrialBoundariesPaidRolesAndLiveSecurityWithoutTracking() {
        await using FoodDiaryDbContext context = await fixture.CreateDbContextAsync();
        DateTime start = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        Role member = await context.Roles.FirstOrDefaultAsync(role => role.Name == RoleNames.Dietologist) ?? Role.Create(RoleNames.Dietologist);
        Role premium = await context.Roles.FirstOrDefaultAsync(role => role.Name == RoleNames.Premium) ?? Role.Create(RoleNames.Premium);
        var trial = User.Create($"trial-principal-{Guid.NewGuid():N}@example.com", "credential-must-not-be-loaded");
        var paid = User.Create($"paid-principal-{Guid.NewGuid():N}@example.com", "credential-must-not-be-loaded");
        trial.ReplaceRoles([member]);
        paid.ReplaceRoles([member, premium]);
        trial.StartPremiumTrial(start, TimeSpan.FromDays(1));
        paid.StartPremiumTrial(start, TimeSpan.FromDays(1));
        context.Users.AddRange(trial, paid);
        await context.SaveChangesAsync();
        int tracked = context.ChangeTracker.Entries().Count();
        var reader = new UserSessionPrincipalReader(context.Users);
        foreach (int offset in new[] { -1, 0, 23, 24, 25 }) {
            DateTime now = start.AddHours(offset);
            Result<UserSessionPrincipalModel> free = await reader.GetCurrentAsync(trial.Id, now);
            Result<UserSessionPrincipalModel> subscribed = await reader.GetCurrentAsync(paid.Id, now);
            Assert.True(free.IsSuccess);
            Assert.True(subscribed.IsSuccess);
            bool activeTrial = offset is >= 0 and < 24;
            Assert.Equal(activeTrial, free.Value.Roles.Contains(RoleNames.Premium, StringComparer.Ordinal));
            Assert.Contains(RoleNames.Dietologist, free.Value.Roles, StringComparer.Ordinal);
            Assert.Equal(activeTrial ? start.AddDays(1) : (DateTime?)null, free.Value.AccessTokenCapUtc);
            Assert.Contains(RoleNames.Premium, subscribed.Value.Roles, StringComparer.Ordinal);
            Assert.Null(subscribed.Value.AccessTokenCapUtc);
        }
        Assert.Equal(tracked, context.ChangeTracker.Entries().Count());

        await context.UserRoles.Where(role => role.UserId == paid.Id && role.RoleId == premium.Id).ExecuteDeleteAsync();
        await context.Users.Where(user => user.Id == paid.Id).ExecuteUpdateAsync(setters =>
            setters.SetProperty(user => user.SecurityVersion, user => user.SecurityVersion + 1));
        Result<UserSessionPrincipalModel> fresh = await reader.GetCurrentAsync(paid.Id, start.AddDays(2));
        Assert.True(fresh.IsSuccess);
        Assert.DoesNotContain(RoleNames.Premium, fresh.Value.Roles, StringComparer.Ordinal);
        Assert.Equal(paid.SecurityVersion + 1, fresh.Value.SecurityVersion);
        Assert.True(paid.HasRole(RoleNames.Premium));
        await context.Users.Where(user => user.Id == paid.Id).ExecuteUpdateAsync(setters => setters.SetProperty(user => user.IsActive, valueExpression: false));
        Assert.True((await reader.GetCurrentAsync(paid.Id, start)).IsFailure);
        await context.Users.Where(user => user.Id == paid.Id).ExecuteUpdateAsync(setters =>
            setters.SetProperty(user => user.IsActive, valueExpression: true).SetProperty(user => user.DeletedAt, start));
        Assert.True((await reader.GetCurrentAsync(paid.Id, start)).IsFailure);
        Assert.Equal(tracked, context.ChangeTracker.Entries().Count());
    }
}
