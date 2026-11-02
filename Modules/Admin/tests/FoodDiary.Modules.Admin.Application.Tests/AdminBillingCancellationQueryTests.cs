using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminBillingCancellations;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminBillingCancellationQueryTests {
    [Fact]
    public async Task Report_NormalizesFiltersAndReturnsTheRepositoryReport() {
        IAdminBillingQuery billing = Substitute.For<IAdminBillingQuery>();
        var report = new AdminBillingCancellationReportReadModel([], 1, 20, 0, 0, 0, []);
        using var cancellation = new CancellationTokenSource();
        billing.GetCancellationsAsync(Arg.Any<AdminBillingListFilter>(), cancellation.Token).Returns(report);
        DateTime from = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Unspecified);

        Result<AdminBillingCancellationReportReadModel> result = await new GetAdminBillingCancellationsQueryHandler(billing)
            .Handle(new GetAdminBillingCancellationsQuery(0, 0, " PADDLE ", " CANCELED ", " example ", from, from.AddDays(1)), cancellation.Token);

        Assert.Same(report, ResultAssert.Success(result));
        await billing.Received(1).GetCancellationsAsync(Arg.Is<AdminBillingListFilter>(filter =>
            filter.Page == 1 && filter.Limit == 20 && filter.Provider == BillingProviderNames.Paddle && filter.Status == "canceled"
            && filter.Kind == null && filter.Search == "example" && filter.FromUtc == DateTime.SpecifyKind(from, DateTimeKind.Utc)
            && filter.ToUtc == DateTime.SpecifyKind(from.AddDays(1), DateTimeKind.Utc)), cancellation.Token);
    }
}
