using System.Globalization;
using System.Text.Json;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Application.Commands.CreateCheckoutSession;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Domain.ValueObjects;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfile;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;
using FoodDiary.Testing.Assertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class SharedBillingContextIntegrationTests {
    [RequiresDockerFact]
    public async Task StarsCheckout_ResumeSurvivesNewContextAndIgnoresUnrelatedOrdersAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.CreateTelegram(123, "synthetic");
        var otherUser = User.CreateTelegram(124, "synthetic-other");
        context.Users.AddRange(user, otherUser);
        await context.SaveChangesAsync();
        await using ServiceProvider firstProvider = CreateProvider(context);
        ISender users = WorkflowUsers(user);
        users.Send(new GetUserBillingProfileQuery(user.Id), Arg.Any<CancellationToken>()).Returns(Result.Success(
            new UserBillingProfileModel(user.Id, Email: null, IsActive: true, IsDeleted: false, HasPaidPremium: false,
                PremiumTrialStartedAtUtc: null, PremiumTrialEndsAtUtc: null, TelegramUserId: 123)));
        IBillingProviderGateway gateway = Substitute.For<IBillingProviderGateway>();
        gateway.Provider.Returns(BillingProviderNames.TelegramStars);
        var quoted = new BillingCheckoutSessionModel("invoice_resume", "https://t.me/$resume",
            "123:" + user.Id.Value.ToString("N", CultureInfo.InvariantCulture), "stars_test_5m_500", "monthly", 500, "XTR", 300);
        gateway.CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(quoted));
        IBillingProviderGatewayAccessor gateways = Substitute.For<IBillingProviderGatewayAccessor>();
        gateways.GetProviderOrDefault(BillingProviderNames.TelegramStars).Returns(gateway);
        CreateCheckoutSessionCommandHandler Handler(ServiceProvider provider) => new(users,
            provider.GetRequiredService<IBillingSubscriptionWriteRepository>(), provider.GetRequiredService<IBillingPaymentWriteRepository>(),
            gateways, TimeProvider.System, provider.GetRequiredService<IBillingCheckoutLock>(), provider.GetRequiredService<IBillingTransactionRunner>());

        ResultAssert.Success(await Handler(firstProvider).Handle(
            new CreateCheckoutSessionCommand(user.Id.Value, "monthly", BillingProviderNames.TelegramStars, "first"), CancellationToken.None));
        BillingSubscription before = await context.BillingSubscriptions.AsNoTracking().SingleAsync();
        context.BillingPayments.AddRange(
            BillingPayment.CreateWithFinancials(otherUser.Id, billingSubscriptionId: null, BillingProviderNames.TelegramStars,
                "other_user_invoice", "other", externalSubscriptionId: null, externalPaymentMethodId: null, "price", "monthly",
                BillingSubscription.PendingCheckoutStatus, BillingPaymentKinds.Checkout, BillingPaymentFinancials.FromFields(500, "XTR"),
                currentPeriodStartUtc: null, currentPeriodEndUtc: null, webhookEventId: null, providerMetadataJson: null),
            BillingPayment.CreateWithFinancials(user.Id, billingSubscriptionId: null, BillingProviderNames.TelegramStars,
                "unrelated_invoice", quoted.CustomerId, externalSubscriptionId: null, externalPaymentMethodId: null, "price", "monthly",
                BillingSubscription.PendingCheckoutStatus, BillingPaymentKinds.Checkout, BillingPaymentFinancials.FromFields(500, "XTR"),
                currentPeriodStartUtc: null, currentPeriodEndUtc: null, webhookEventId: null, providerMetadataJson: null));
        await context.SaveChangesAsync();

        await using FoodDiaryDbContext second = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        await using ServiceProvider secondProvider = CreateProvider(second);
        BillingCheckoutSessionModel resumed = ResultAssert.Success(await Handler(secondProvider).Handle(
            new CreateCheckoutSessionCommand(user.Id.Value, "monthly", BillingProviderNames.TelegramStars, "retry"), CancellationToken.None));
        BillingSubscription after = await second.BillingSubscriptions.AsNoTracking().SingleAsync();
        BillingPayment saved = await second.BillingPayments.AsNoTracking().SingleAsync(payment => payment.ExternalPaymentId == quoted.SessionId);
        using var metadata = JsonDocument.Parse(Assert.IsType<string>(saved.ProviderMetadataJson));
        Assert.Multiple(() => {
            Assert.Equal(quoted, resumed);
            Assert.Equal(before.ModifiedOnUtc, after.ModifiedOnUtc);
            Assert.Equal(quoted.Url, metadata.RootElement.GetProperty("invoiceUrl").GetString());
            Assert.Equal(3, second.BillingPayments.Count());
        });
        await gateway.Received(1).CreateCheckoutSessionAsync(Arg.Any<BillingCheckoutSessionRequestModel>(), Arg.Any<CancellationToken>());
    }
}
