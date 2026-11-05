using System.Globalization;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.ProcessTelegramStarsUpdate;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Domain.ValueObjects;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Commands.EnsureUserPremiumRole;
using FoodDiary.Modules.Users.Contracts.Commands.RemoveUserPremiumRole;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;
using FoodDiary.Modules.Users.Contracts.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class SharedBillingContextIntegrationTests {
    [RequiresDockerFact]
    public async Task Stars_ConcurrentDeliveryCommitsOnePaymentAndTimedRole_WithStrictPersistedExpiryAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.CreateTelegram(123, "synthetic");
        Role premium = await context.Roles.FirstOrDefaultAsync(role => role.Name == RoleNames.Premium) ?? Role.Create(RoleNames.Premium);
        if (context.Entry(premium).State == EntityState.Detached) {
            context.Roles.Add(premium);
        }
        context.Users.Add(user);
        await using ServiceProvider first = CreateProvider(context, includeUsers: true);
        string customer = "123:" + user.Id.Value.ToString("N", CultureInfo.InvariantCulture);
        var subscription = BillingSubscription.CreatePending(user.Id, BillingProviderNames.TelegramStars, customer, "stars_30d_500", "monthly");
        await first.GetRequiredService<IBillingSubscriptionWriteRepository>().AddAsync(subscription);
        await first.GetRequiredService<IBillingPaymentWriteRepository>().AddAsync(BillingPayment.CreateWithFinancials(user.Id,
            subscription.TypedId, BillingProviderNames.TelegramStars, "invoice_owned", customer, externalSubscriptionId: null,
            externalPaymentMethodId: null, "stars_30d_500", "monthly", BillingSubscription.PendingCheckoutStatus, BillingPaymentKinds.Checkout,
            BillingPaymentFinancials.FromFields(500, "XTR"), currentPeriodStartUtc: null, currentPeriodEndUtc: null,
            webhookEventId: null, providerMetadataJson: null));
        await first.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        ProcessTelegramStarsUpdateCommandHandler firstHandler = CreateStarsHandler(first);
        var approval = new TelegramStarsUpdateModel(10, 123, "pre_checkout", "invoice_owned", "XTR", 500);
        Result<TelegramStarsUpdateResultModel> approved = await firstHandler.Handle(new ProcessTelegramStarsUpdateCommand(approval), CancellationToken.None);
        ResultAssert.Success(approved);
        Assert.True(approved.Value.CheckoutApproved);
        await using FoodDiaryDbContext secondContext = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        await using ServiceProvider second = CreateProvider(secondContext, includeUsers: true);
        DateTime paidAt = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).UtcDateTime;
        DateTime ends = paidAt.AddDays(30);
        var payment = new TelegramStarsUpdateModel(20, 123, "payment", "invoice_owned", "XTR", 500, "first_charge",
            ends, paidAt, IsRecurring: true, IsFirstRecurring: true);
        Result<TelegramStarsUpdateResultModel>[] results = await Task.WhenAll(
            firstHandler.Handle(new ProcessTelegramStarsUpdateCommand(payment), CancellationToken.None),
            CreateStarsHandler(second).Handle(new ProcessTelegramStarsUpdateCommand(payment), CancellationToken.None));
        foreach (Result<TelegramStarsUpdateResultModel> result in results) {
            ResultAssert.Success(result);
        }
        await using FoodDiaryDbContext verification = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        User saved = await verification.Users.AsNoTracking().SingleAsync(candidate => candidate.Id == user.Id);
        Assert.Multiple(() => {
            Assert.Equal(ends, saved.PremiumAccessEndsAtUtc);
            Assert.Equal(2, verification.BillingPayments.Count());
            Assert.Single(verification.BillingWebhookEvents);
            Assert.Single(verification.UserRoles.Where(role => role.UserId == user.Id));
        });
        IUserSessionPrincipalReader principals = second.GetRequiredService<IUserSessionPrincipalReader>();
        Result<UserSessionPrincipalModel> before = await principals.GetCurrentAsync(user.Id, ends.AddTicks(-10));
        Result<UserSessionPrincipalModel> after = await principals.GetCurrentAsync(user.Id, ends);
        ResultAssert.Success(before);
        ResultAssert.Success(after);
        Assert.Multiple(() => {
            Assert.Contains(RoleNames.Premium, before.Value.Roles, StringComparer.Ordinal);
            Assert.Equal(ends, before.Value.AccessTokenCapUtc);
            Assert.DoesNotContain(RoleNames.Premium, after.Value.Roles, StringComparer.Ordinal);
        });
    }

    private static ProcessTelegramStarsUpdateCommandHandler CreateStarsHandler(ServiceProvider provider) {
        ISender sender = Substitute.For<ISender>();
        IUserBillingProfileReadModelRepository profiles = provider.GetRequiredService<IUserBillingProfileReadModelRepository>();
        IUserRoleMembershipService roles = provider.GetRequiredService<IUserRoleMembershipService>();
        sender.Send(Arg.Any<GetUserBillingProfileIncludingDeletedQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => profiles.GetBillingProfileIncludingDeletedAsync(call.Arg<GetUserBillingProfileIncludingDeletedQuery>().UserId, call.Arg<CancellationToken>()));
        sender.Send(Arg.Any<EnsureUserPremiumRoleCommand>(), Arg.Any<CancellationToken>()).Returns(async call => {
            EnsureUserPremiumRoleCommand command = call.Arg<EnsureUserPremiumRoleCommand>();
            await roles.SetPremiumAccessEndAsync(command.UserId, command.AccessEndsAtUtc, call.Arg<CancellationToken>());
            await roles.EnsureRoleAsync(command.UserId, RoleNames.Premium, call.Arg<CancellationToken>());
            return Unit.Value;
        });
        sender.Send(Arg.Any<RemoveUserPremiumRoleCommand>(), Arg.Any<CancellationToken>()).Returns(async call => {
            await roles.RemoveRoleAsync(call.Arg<RemoveUserPremiumRoleCommand>().UserId, RoleNames.Premium, call.Arg<CancellationToken>());
            return Unit.Value;
        });
        IBillingPublicConfigProvider config = Substitute.For<IBillingPublicConfigProvider>();
        config.GetPublicConfig().Returns(new BillingPublicConfigModel(BillingProviderNames.Paddle, PaddleClientToken: null,
            [BillingProviderNames.TelegramStars], TelegramStarsPrice: 500));
        IBillingSubscriptionWriteRepository subscriptions = provider.GetRequiredService<IBillingSubscriptionWriteRepository>();
        return new ProcessTelegramStarsUpdateCommandHandler(provider.GetRequiredService<IBillingPaymentWriteRepository>(), subscriptions,
            provider.GetRequiredService<IBillingWebhookEventWriteRepository>(), provider.GetRequiredService<IBillingTransactionRunner>(),
            sender, new BillingAccessService(sender, subscriptions, TimeProvider.System), config,
            new BillingCancellationRecorder(provider.GetRequiredService<IBillingCancellationRepository>(), TimeProvider.System), TimeProvider.System);
    }
}
