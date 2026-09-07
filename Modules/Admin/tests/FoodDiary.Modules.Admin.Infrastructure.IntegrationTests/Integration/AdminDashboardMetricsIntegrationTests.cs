using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Admin;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class AdminDashboardMetricsIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task Metrics_KeepCurrenciesSeparateDeduplicatePayersAndRespectExclusiveEnd() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        DateTime start = new(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var user = User.Create($"dashboard-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        context.Entry(user).Property(item => item.CreatedOnUtc).CurrentValue = start;
        context.BillingPayments.AddRange(Payment(user.Id, start, "USD", 10), Payment(user.Id, start.AddDays(1), "EUR", 20),
            Payment(user.Id, start.AddDays(3), "USD", 999), Payment(user.Id, start, "USD", 100, "failed"));
        var usage = AiUsage.Create(user.Id, "test", "test", 100, 50, 150);
        context.AiUsages.Add(usage);
        context.Entry(usage).Property(item => item.CreatedOnUtc).CurrentValue = start;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reader = new AdminDashboardMetricsReader(context);
        AdminDashboardMetrics result = await reader.GetAsync(start, start.AddDays(3), monthly: false, CancellationToken.None);
        Assert.Equal(1, result.Registrations);
        Assert.Equal(1, result.PayingUsers);
        Assert.Equal(150, result.AiTokens);
        Assert.Equal(3, result.Trend.Count);
        Assert.Equal(new AdminDashboardRevenuePoint("USD", 10), Assert.Single(result.Trend[0].Revenue));
        Assert.Equal(new AdminDashboardRevenuePoint("EUR", 20), Assert.Single(result.Trend[1].Revenue));
        Assert.Empty(result.Trend[2].Revenue);
        Assert.Empty(context.ChangeTracker.Entries());
        AdminDashboardMetrics monthly = await reader.GetAsync(start, start.AddDays(3), monthly: true, CancellationToken.None);
        Assert.Equal(2, Assert.Single(monthly.Trend).Revenue.Count);
    }

    private static BillingPayment Payment(UserId userId, DateTime occurred, string currency, decimal amount, string status = "completed") =>
        BillingPayment.Create(userId, billingSubscriptionId: null, provider: "paddle", externalPaymentId: Guid.NewGuid().ToString(),
            externalCustomerId: null, externalSubscriptionId: null, externalPaymentMethodId: null, externalPriceId: null, plan: null,
            status, kind: BillingPaymentKinds.Transaction, amount, currency, currentPeriodStartUtc: null, currentPeriodEndUtc: null,
            webhookEventId: null, providerMetadataJson: null, occurredAtUtc: occurred);
}
