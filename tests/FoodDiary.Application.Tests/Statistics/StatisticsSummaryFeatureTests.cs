using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Application.Statistics.Queries.GetStatisticsSummary;
using FoodDiary.Application.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Statistics;

[ExcludeFromCodeCoverage]
public sealed class StatisticsSummaryFeatureTests {
    [Fact]
    public async Task GetStatisticsSummaryQueryValidator_WithNonPositiveQuantization_Fails() {
        var validator = new GetStatisticsSummaryQueryValidator();
        var query = new GetStatisticsSummaryQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 0);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetStatisticsSummaryQueryHandler_ReturnsNutritionWeightAndWaist() {
        var user = User.Create("statistics-summary@example.com", "hash");
        var from = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 8, 7, 23, 59, 59, DateTimeKind.Utc);
        IDashboardStatisticsReadService statisticsReadService = Substitute.For<IDashboardStatisticsReadService>();
        IWeightEntryReadService weightReadService = Substitute.For<IWeightEntryReadService>();
        IWaistEntryReadService waistReadService = Substitute.For<IWaistEntryReadService>();
        statisticsReadService
            .GetStatisticsAsync(user.Id, from, to, 1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([
                new DashboardStatisticsBucketReadModel(from, to, 1800, 120, 70, 160, 20),
            ])));
        weightReadService
            .GetSummariesAsync(user.Id, from.Date, to.Date, 1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WeightEntrySummaryModel>>([new WeightEntrySummaryModel(from.Date, to.Date, 75.3)]));
        waistReadService
            .GetSummariesAsync(user.Id, from.Date, to.Date, 1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WaistEntrySummaryModel>>([new WaistEntrySummaryModel(from.Date, to.Date, 82.1)]));
        var handler = new GetStatisticsSummaryQueryHandler(
            statisticsReadService,
            weightReadService,
            waistReadService,
            CreateCurrentUserAccessService());

        Result<StatisticsSummaryModel> result = await handler.Handle(
            new GetStatisticsSummaryQuery(user.Id.Value, from, to, 1),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(1800, Assert.Single(result.Value.Nutrition).TotalCalories),
            () => Assert.Equal(75.3, Assert.Single(result.Value.Weight).AverageWeight),
            () => Assert.Equal(82.1, Assert.Single(result.Value.Waist).AverageCircumference));
    }

    private static ICurrentUserAccessService CreateCurrentUserAccessService() {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(null));

        return service;
    }
}
