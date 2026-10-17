using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Contracts.Commands.RemoveUserPremiumRole;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class SharedBillingContextIntegrationTests {
    [RequiresDockerTheory]
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    [InlineData(true, true, true)]
    public async Task EndedPremium_ForInaccessibleAccount_RevokesOnlyOwnedRoleInSameTransaction(
        bool deleted, bool managed, bool rollback) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        DateTime now = DateTime.UtcNow;
        Role premium = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        var user = User.Create("billing-role-lifecycle@example.com", "hash");
        user.ReplaceRoles([premium]);
        if (deleted) {
            user.DeleteAccount(now);
        } else {
            user.Deactivate(now);
        }
        long initialSecurityVersion = user.SecurityVersion;
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateProvider(context, includeUsers: true);
        IBillingSubscriptionWriteRepository subscriptions = provider.GetRequiredService<IBillingSubscriptionWriteRepository>();
        IBillingTransactionRunner transaction = provider.GetRequiredService<IBillingTransactionRunner>();
        IUserRoleMembershipService memberships = provider.GetRequiredService<IUserRoleMembershipService>();
        ISender users = Substitute.For<ISender>();
        users.Send(Arg.Is<RemoveUserPremiumRoleCommand>(command => command.UserId == user.Id), Arg.Any<CancellationToken>())
            .Returns(async call => {
                await memberships.RemoveRoleAsync(user.Id, RoleNames.Premium, call.Arg<CancellationToken>());
                return Unit.Value;
            });
        var subscription = BillingSubscription.CreatePending(user.Id, BillingProviderNames.YooKassa, "lifecycle_customer", "price", "monthly");
        subscription.ApplyProviderSnapshot(BillingProviderNames.YooKassa, "lifecycle_payment", "method", "price", "monthly",
            "canceled", now.AddMonths(-1), now, cancelAtPeriodEnd: false, canceledAtUtc: now,
            trialStartUtc: null, trialEndUtc: null, "lifecycle_event", now, webhookOccurredAtUtc: now);
        subscription.MarkPremiumRoleManagedByBilling(managed, now);
        await transaction.ExecuteAsync(async token => await subscriptions.AddAsync(subscription, token));
        var syncer = new BillingWebhookPremiumRoleSyncer(new BillingAccessService(users, subscriptions, TimeProvider.System),
            Substitute.For<ISender>());
        var profile = new UserBillingProfileModel(user.Id, user.Email, user.IsActive, deleted,
            HasPaidPremium: true, PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null);
        BillingWebhookEventModel webhook = WorkflowWebhook(user, "lifecycle_event", "lifecycle_payment", now) with { Status = "canceled" };

        async Task SynchronizeAsync(CancellationToken token) {
            await syncer.SyncAsync(profile, subscription, webhook, token);
            if (rollback) {
                throw new InvalidOperationException("Force rollback after role revocation.");
            }
        }
        if (rollback) {
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.ExecuteAsync(SynchronizeAsync));
        } else {
            await transaction.ExecuteAsync(SynchronizeAsync);
        }

        await using FoodDiaryDbContext verification = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        User restored = await verification.Users.Include(candidate => candidate.UserRoles).ThenInclude(membership => membership.Role)
            .SingleAsync(candidate => candidate.Id == user.Id);
        if (deleted) {
            restored.Restore(now.AddMinutes(1));
        } else {
            restored.Activate(now.AddMinutes(1));
        }
        BillingSubscription persisted = await verification.BillingSubscriptions.AsNoTracking().SingleAsync(item => item.UserId == user.Id);
        bool revoked = managed && !rollback;
        Assert.Multiple(
            () => Assert.Equal(!revoked, restored.HasRole(RoleNames.Premium)),
            () => Assert.Equal(managed && rollback, persisted.PremiumRoleManagedByBilling),
            () => Assert.Equal(initialSecurityVersion + (revoked ? 2 : 1), restored.SecurityVersion));
    }
}
