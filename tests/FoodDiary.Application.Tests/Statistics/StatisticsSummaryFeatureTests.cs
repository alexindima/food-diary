using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
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

    [Fact]
    public async Task GetStatisticsSummaryQueryHandler_WhenCurrentUserAccessFails_ReturnsFailureWithoutReadingStatistics() {
        IDashboardStatisticsReadService statisticsReadService = Substitute.For<IDashboardStatisticsReadService>();
        IWeightEntryReadService weightReadService = Substitute.For<IWeightEntryReadService>();
        IWaistEntryReadService waistReadService = Substitute.For<IWaistEntryReadService>();
        ICurrentUserAccessService accessService = Substitute.For<ICurrentUserAccessService>();
        Error accessError = Errors.Validation.Invalid("UserId", "Access denied.");
        accessService
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(accessError));
        var handler = new GetStatisticsSummaryQueryHandler(
            statisticsReadService,
            weightReadService,
            waistReadService,
            accessService);

        Result<StatisticsSummaryModel> result = await handler.Handle(
            new GetStatisticsSummaryQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1),
            CancellationToken.None);

        ResultAssert.Failure(result, accessError.Code);
        await statisticsReadService.DidNotReceiveWithAnyArgs().GetStatisticsAsync(
            default,
            default,
            default,
            default,
            default);
    }

    [Fact]
    public async Task GetStatisticsSummaryQueryHandler_WhenDateRangeIsInverted_ReturnsValidationFailure() {
        var handler = new GetStatisticsSummaryQueryHandler(
            Substitute.For<IDashboardStatisticsReadService>(),
            Substitute.For<IWeightEntryReadService>(),
            Substitute.For<IWaistEntryReadService>(),
            CreateCurrentUserAccessService());
        DateTime date = DateTime.UtcNow;

        Result<StatisticsSummaryModel> result = await handler.Handle(
            new GetStatisticsSummaryQuery(Guid.NewGuid(), date, date.AddDays(-1), 1),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        Assert.Contains(nameof(GetStatisticsSummaryQuery.DateFrom), result.Error.Details!.Keys, StringComparer.Ordinal);
    }

    [Fact]
    public async Task GetStatisticsSummaryQueryHandler_WhenQuantizationIsNonPositive_ReturnsValidationFailure() {
        var handler = new GetStatisticsSummaryQueryHandler(
            Substitute.For<IDashboardStatisticsReadService>(),
            Substitute.For<IWeightEntryReadService>(),
            Substitute.For<IWaistEntryReadService>(),
            CreateCurrentUserAccessService());

        Result<StatisticsSummaryModel> result = await handler.Handle(
            new GetStatisticsSummaryQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 0),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        Assert.Contains(nameof(GetStatisticsSummaryQuery.QuantizationDays), result.Error.Details!.Keys, StringComparer.Ordinal);
    }

    [Fact]
    public async Task GetStatisticsSummaryQueryHandler_WhenStatisticsReadFails_ReturnsFailureWithoutReadingBodyMeasurements() {
        IDashboardStatisticsReadService statisticsReadService = Substitute.For<IDashboardStatisticsReadService>();
        IWeightEntryReadService weightReadService = Substitute.For<IWeightEntryReadService>();
        IWaistEntryReadService waistReadService = Substitute.For<IWaistEntryReadService>();
        var error = new Error("Statistics.Unavailable", "Statistics unavailable.");
        statisticsReadService
            .GetStatisticsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), 1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<IReadOnlyList<DashboardStatisticsBucketReadModel>>(error)));
        var handler = new GetStatisticsSummaryQueryHandler(
            statisticsReadService, weightReadService, waistReadService, CreateCurrentUserAccessService());

        Result<StatisticsSummaryModel> result = await handler.Handle(
            new GetStatisticsSummaryQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1),
            CancellationToken.None);

        ResultAssert.Failure(result, error.Code);
        await weightReadService.DidNotReceiveWithAnyArgs().GetSummariesAsync(default, default, default, default, default);
        await waistReadService.DidNotReceiveWithAnyArgs().GetSummariesAsync(default, default, default, default, default);
    }

    private static ICurrentUserAccessService CreateCurrentUserAccessService() {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(null));

        return service;
    }
}
