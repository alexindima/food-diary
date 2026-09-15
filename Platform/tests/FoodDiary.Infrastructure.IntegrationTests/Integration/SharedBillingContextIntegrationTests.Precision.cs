using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class SharedBillingContextIntegrationTests {
    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PaymentPrecision_PreservesAmountsOnCleanAndUpgradedDatabaseAsync(bool upgrade) {
        await using FoodDiaryDbContext context = databaseFixture.CreateDbContext(await databaseFixture.CreateIsolatedDatabaseAsync());
        IMigrator migrator = context.GetService<IMigrator>();
        const string previousMigration = "20260914032832_ProtectAiPromptConcurrentUpdates";
        await migrator.MigrateAsync(upgrade ? previousMigration : null);
        var user = User.Create("billing-precision@example.com", "hash");
        context.Users.Add(user);
        const decimal previousMaximum = 9_999_999_999_999_999.99m;
        var payment = BillingPayment.Create(user.Id, billingSubscriptionId: null, BillingProviderNames.Stripe, "in_precision",
            externalCustomerId: null, externalSubscriptionId: null, externalPaymentMethodId: null,
            externalPriceId: null, plan: null, "completed", BillingPaymentKinds.Transaction, previousMaximum, "BHD",
            currentPeriodStartUtc: null, currentPeriodEndUtc: null, webhookEventId: null, providerMetadataJson: null,
            tax: -previousMaximum, fee: 0.01m, earnings: previousMaximum, payoutCurrency: "BHD", payoutEarnings: previousMaximum);
        context.Set<BillingPayment>().Add(payment);
        await context.SaveChangesAsync();
        if (upgrade) {
            await migrator.MigrateAsync();
        }
        context.ChangeTracker.Clear();
        BillingPayment saved = await context.Set<BillingPayment>().SingleAsync();
        Assert.Equal(previousMaximum, saved.Amount);
        Assert.Equal(-previousMaximum, saved.Tax);
        Assert.Equal(previousMaximum, saved.PayoutEarnings);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        await migrator.MigrateAsync(previousMigration);
        await migrator.MigrateAsync();

        saved.ApplyProviderResult(billingSubscriptionId: null, externalCustomerId: null, externalSubscriptionId: null,
            externalPaymentMethodId: null, externalPriceId: null, plan: null, "completed", BillingPaymentKinds.Transaction,
            7.991m, "BHD", currentPeriodStartUtc: null, currentPeriodEndUtc: null, "evt_precision", providerMetadataJson: null,
            tax: 0.001m, fee: -0.001m, earnings: 7.989m, payoutCurrency: "BHD", payoutEarnings: 7.989m);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        BillingPayment updated = await context.Set<BillingPayment>().SingleAsync();
        Assert.Multiple(
            () => Assert.Equal(7.991m, updated.Amount),
            () => Assert.Equal(0.001m, updated.Tax),
            () => Assert.Equal(-0.001m, updated.Fee),
            () => Assert.Equal(7.989m, updated.Earnings),
            () => Assert.Equal(7.989m, updated.PayoutEarnings));

        PostgresException failure = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync(previousMigration));
        Assert.Contains("Cannot reduce billing precision", failure.MessageText, StringComparison.Ordinal);
        Assert.Equal(7.991m, await context.Set<BillingPayment>().Select(item => item.Amount).SingleAsync());
    }
}
