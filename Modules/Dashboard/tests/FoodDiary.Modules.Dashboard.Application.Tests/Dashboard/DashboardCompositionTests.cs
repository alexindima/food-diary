using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dashboard;
using FoodDiary.Application.Statistics;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;
using FoodDiary.Results;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Tests.Dashboard;

[ExcludeFromCodeCoverage]
public sealed class DashboardCompositionTests {
    [Fact]
    public async Task Statistics_WithoutAReader_FailsResolutionInsteadOfRecursing() {
        await using ServiceProvider provider = CreateServices().BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<ISender>().Send(CreateQuery()));

        Assert.Contains(nameof(IDashboardStatisticsReadService), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Statistics_WithAnExplicitReader_InvokesItOnceThroughTheRealMediator() {
        IServiceCollection services = CreateServices();
        IDashboardStatisticsReadService reader = Substitute.For<IDashboardStatisticsReadService>();
        reader.GetStatisticsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([]));
        services.AddScoped(_ => reader);
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        GetStatisticsQuery query = CreateQuery();
        using var cancellation = new CancellationTokenSource();

        Result<IReadOnlyList<AggregatedStatisticsModel>> result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(query, cancellation.Token);

        Assert.Empty(ResultAssert.Success(result));
        await reader.Received(1).GetStatisticsAsync(
            new UserId(query.UserId!.Value), query.DateFrom, query.DateTo, query.QuantizationDays, cancellation.Token);
    }

    private static ServiceCollection CreateServices() {
        var services = new ServiceCollection();
        services.AddDashboardModule();
        services.AddStatisticsModule();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Error?>(null));
        services.AddScoped(_ => access);
        return services;
    }

    private static GetStatisticsQuery CreateQuery() => new(
        Guid.NewGuid(), new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc), 1);
}
