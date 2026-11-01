using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.ReadModel.Composition.Admin;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Admin.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class AdminBillingCancellationRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task Cancellations_FilterBeforePagingCountReasonsAndDeletePrivateFeedbackWithUser() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await VerifyFilteredReportAsync(context);
    }

    // Shared with the owned local PostgreSQL verification when Docker Desktop is unavailable.
    public static async Task VerifyFilteredReportAsync(FoodDiaryDbContext context) {
        var from = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime to = from.AddDays(3);
        var user = User.Create($"cancellation-{Guid.NewGuid():N}@example.test", "synthetic-hash");
        context.Users.Add(user);
        context.BillingCancellations.AddRange(
            CreateCancellation(user, from, "too_expensive", "Price"),
            CreateCancellation(user, from.AddDays(1), reason: null, comment: null),
            CreateCancellation(user, from.AddDays(2), "too_expensive", "<script>plain text</script>"),
            CreateCancellation(user, to, "technical_issues", "Excluded boundary"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new AdminBillingRepository(context);
        var filter = new AdminBillingListFilter(1, 1, BillingProviderNames.Paddle, "scheduled", Kind: null, user.Email, from, to);
        AdminBillingCancellationReportReadModel report = await repository.GetCancellationsAsync(filter);
        AdminBillingCancellationReadModel row = Assert.Single(report.Items);
        Assert.Multiple(() => {
            Assert.Equal(3, report.TotalItems);
            Assert.Equal(3, report.TotalPages);
            Assert.Equal(2, report.FeedbackCount);
            Assert.Equal(2, report.Reasons.Single(item => string.Equals(item.ReasonCode, "too_expensive", StringComparison.Ordinal)).Count);
            Assert.Equal(1, report.Reasons.Single(item => item.ReasonCode is null).Count);
            Assert.Equal(from.AddDays(2), row.RequestedAtUtc);
            Assert.Equal("<script>plain text</script>", row.Comment);
            Assert.Equal(user.Id.Value, row.UserId);
            Assert.Equal("fooddiary", row.Source);
            Assert.Equal("monthly", row.Plan);
        });
        AdminBillingCancellationReportReadModel secondPage = await repository.GetCancellationsAsync(filter with { Page = 2 });
        Assert.Null(Assert.Single(secondPage.Items).ReasonCode);
        AdminBillingCancellationReportReadModel wrongProvider = await repository.GetCancellationsAsync(filter with { Provider = BillingProviderNames.Stripe });
        Assert.Empty(wrongProvider.Items);
        Assert.Equal(0, wrongProvider.TotalItems);
        Assert.Empty(context.ChangeTracker.Entries());
        User storedUser = await context.Users.SingleAsync(item => item.Id == user.Id);
        context.Users.Remove(storedUser);
        await context.SaveChangesAsync();
        Assert.False(await context.BillingCancellations.AnyAsync(item => item.UserId == user.Id));
    }

    private static BillingCancellation CreateCancellation(User user, DateTime requestedAtUtc, string? reason, string? comment) {
        var subscription = BillingSubscription.CreatePending(user.Id, BillingProviderNames.Paddle, "ctm_owned", "pri_monthly", "monthly");
        subscription.ApplyProviderSnapshot(BillingProviderNames.Paddle, "sub_" + Guid.NewGuid().ToString("N"), externalPaymentMethodId: null,
            "pri_monthly", "monthly", "active", requestedAtUtc, requestedAtUtc.AddDays(30), cancelAtPeriodEnd: true,
            canceledAtUtc: null, trialStartUtc: null, trialEndUtc: null, "evt_synthetic", requestedAtUtc);
        var cancellation = BillingCancellation.Create(subscription, "fooddiary", requestedAtUtc, requestedAtUtc.AddDays(30));
        cancellation.SubmitFeedback(reason, comment, requestedAtUtc);
        return cancellation;
    }
}
