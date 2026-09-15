using FoodDiary.Modules.Dashboard.Contracts.Queries.ReadDashboardStatistics;
using FoodDiary.Testing;
using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries;
using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Dashboard.Contracts.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Application.Statistics.Queries.GetStatisticsSummary;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
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
    public async Task GetStatisticsSummaryQueryValidator_WithExcessivePeriodAndQuantization_Fails() {
        var validator = new GetStatisticsSummaryQueryValidator();
        var from = new DateTime(2025, 1, 1);
        var query = new GetStatisticsSummaryQuery(Guid.NewGuid(), from, from.AddDays(366), int.MaxValue);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.Multiple(
            () => Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, nameof(query.DateTo), StringComparison.Ordinal)),
            () => Assert.Contains(result.Errors, error => string.Equals(error.PropertyName, nameof(query.QuantizationDays), StringComparison.Ordinal)));
    }

    [Fact]
    public async Task GetStatisticsSummaryQueryHandler_ReturnsNutritionWeightAndWaist() {
        var user = User.Create("statistics-summary@example.com", "hash");
        var from = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 8, 7, 23, 59, 59, DateTimeKind.Utc);
        ISender statisticsReadService = Substitute.For<ISender>();
        ISender weightReadService = Substitute.For<ISender>();
        ISender waistReadService = Substitute.For<ISender>();
        statisticsReadService
            .Send(Arg.Is<ReadDashboardStatisticsQuery>(query => query.UserId == user.Id && query.DateFrom == from && query.DateTo == to && query.QuantizationDays == 1), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([
                new DashboardStatisticsBucketReadModel(from, to, 1800, 120, 70, 160, 20),
            ])));
        weightReadService.Send(new ReadWeightSummariesQuery(UserId: user.Id, DateFrom: from.Date, DateTo: to.Date, QuantizationDays: 1), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WeightEntrySummaryModel>>([new WeightEntrySummaryModel(from.Date, to.Date, 75.3)]));
        waistReadService.Send(new ReadWaistSummariesQuery(UserId: user.Id, DateFrom: from.Date, DateTo: to.Date, QuantizationDays: 1), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WaistEntrySummaryModel>>([new WaistEntrySummaryModel(from.Date, to.Date, 82.1)]));
        var handler = new GetStatisticsSummaryQueryHandler(RequestTestSender.Route((statisticsReadService, [typeof(ReadDashboardStatisticsQuery)]), (RequestTestSender.Route((weightReadService, [typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries.ReadWeightSummariesQuery)]), (waistReadService, [typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries.ReadWaistSummariesQuery)])), [typeof(ReadWeightSummariesQuery), typeof(ReadWaistSummariesQuery)])), CreateCurrentUserAccessService());

        Result<StatisticsSummaryModel> result = await handler.Handle(
            new GetStatisticsSummaryQuery(user.Id.Value, from, to, 1),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(1800, Assert.Single(result.Value.Nutrition).TotalCalories),
            () => Assert.Equal(75.3, Assert.Single(result.Value.Weight).AverageWeightKg),
            () => Assert.Equal(82.1, Assert.Single(result.Value.Waist).AverageCircumferenceCm));
    }

    [Fact]
    public async Task GetStatisticsSummaryQueryHandler_WhenCurrentUserAccessFails_ReturnsFailureWithoutReadingStatistics() {
        ISender statisticsReadService = Substitute.For<ISender>();
        ISender weightReadService = Substitute.For<ISender>();
        ISender waistReadService = Substitute.For<ISender>();
        ICurrentUserAccessService accessService = Substitute.For<ICurrentUserAccessService>();
        Error accessError = Errors.Validation.Invalid("UserId", "Access denied.");
        accessService
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(accessError));
        var handler = new GetStatisticsSummaryQueryHandler(RequestTestSender.Route((statisticsReadService, [typeof(ReadDashboardStatisticsQuery)]), (RequestTestSender.Route((weightReadService, [typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries.ReadWeightSummariesQuery)]), (waistReadService, [typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries.ReadWaistSummariesQuery)])), [typeof(ReadWeightSummariesQuery), typeof(ReadWaistSummariesQuery)])), accessService);

        Result<StatisticsSummaryModel> result = await handler.Handle(
            new GetStatisticsSummaryQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1),
            CancellationToken.None);

        ResultAssert.Failure(result, accessError.Code);
        await statisticsReadService.DidNotReceiveWithAnyArgs().Send(Arg.Is<ReadDashboardStatisticsQuery>(query => query.UserId == default && query.DateFrom == default && query.DateTo == default && query.QuantizationDays == default), default);
    }

    [Fact]
    public async Task GetStatisticsSummaryQueryHandler_WhenDateRangeIsInverted_ReturnsValidationFailure() {
        var handler = new GetStatisticsSummaryQueryHandler(RequestTestSender.Route((Substitute.For<ISender>(), [typeof(ReadDashboardStatisticsQuery)]), (RequestTestSender.Route((Substitute.For<ISender>(), [typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries.ReadWeightSummariesQuery)]), (Substitute.For<ISender>(), [typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries.ReadWaistSummariesQuery)])), [typeof(ReadWeightSummariesQuery), typeof(ReadWaistSummariesQuery)])), CreateCurrentUserAccessService());
        DateTime date = DateTime.UtcNow;

        Result<StatisticsSummaryModel> result = await handler.Handle(
            new GetStatisticsSummaryQuery(Guid.NewGuid(), date, date.AddDays(-1), 1),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        Assert.Contains(nameof(GetStatisticsSummaryQuery.DateFrom), result.Error.Details!.Keys, StringComparer.Ordinal);
    }

    [Fact]
    public async Task GetStatisticsSummaryQueryHandler_WhenQuantizationIsNonPositive_ReturnsValidationFailure() {
        var handler = new GetStatisticsSummaryQueryHandler(RequestTestSender.Route((Substitute.For<ISender>(), [typeof(ReadDashboardStatisticsQuery)]), (RequestTestSender.Route((Substitute.For<ISender>(), [typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries.ReadWeightSummariesQuery)]), (Substitute.For<ISender>(), [typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries.ReadWaistSummariesQuery)])), [typeof(ReadWeightSummariesQuery), typeof(ReadWaistSummariesQuery)])), CreateCurrentUserAccessService());

        Result<StatisticsSummaryModel> result = await handler.Handle(
            new GetStatisticsSummaryQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 0),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        Assert.Contains(nameof(GetStatisticsSummaryQuery.QuantizationDays), result.Error.Details!.Keys, StringComparer.Ordinal);
    }

    [Theory]
    [InlineData(366, 1)]
    [InlineData(1, 367)]
    [InlineData(1, int.MaxValue)]
    public async Task GetStatisticsSummaryQueryHandler_WhenTemporalRangeIsUnsupported_ReturnsValidationFailure(
        int periodDays,
        int quantizationDays) {
        var handler = new GetStatisticsSummaryQueryHandler(RequestTestSender.Route((Substitute.For<ISender>(), [typeof(ReadDashboardStatisticsQuery)]), (RequestTestSender.Route((Substitute.For<ISender>(), [typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries.ReadWeightSummariesQuery)]), (Substitute.For<ISender>(), [typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries.ReadWaistSummariesQuery)])), [typeof(ReadWeightSummariesQuery), typeof(ReadWaistSummariesQuery)])), CreateCurrentUserAccessService());
        var from = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Result<StatisticsSummaryModel> result = await handler.Handle(
            new GetStatisticsSummaryQuery(Guid.NewGuid(), from, from.AddDays(periodDays), quantizationDays),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
    }

    [Fact]
    public async Task GetStatisticsSummaryQueryHandler_WhenStatisticsReadFails_ReturnsFailureWithoutReadingBodyMeasurements() {
        ISender statisticsReadService = Substitute.For<ISender>();
        ISender weightReadService = Substitute.For<ISender>();
        ISender waistReadService = Substitute.For<ISender>();
        var error = new Error("Statistics.Unavailable", "Statistics unavailable.");
        statisticsReadService
            .Send(Arg.Is<ReadDashboardStatisticsQuery>(query => query.QuantizationDays == 1), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<IReadOnlyList<DashboardStatisticsBucketReadModel>>(error)));
        var handler = new GetStatisticsSummaryQueryHandler(RequestTestSender.Route((statisticsReadService, [typeof(ReadDashboardStatisticsQuery)]), (RequestTestSender.Route((weightReadService, [typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries.ReadWeightSummariesQuery)]), (waistReadService, [typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries.ReadWaistSummariesQuery)])), [typeof(ReadWeightSummariesQuery), typeof(ReadWaistSummariesQuery)])), CreateCurrentUserAccessService());

        Result<StatisticsSummaryModel> result = await handler.Handle(
            new GetStatisticsSummaryQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1),
            CancellationToken.None);

        ResultAssert.Failure(result, error.Code);
        await weightReadService.DidNotReceiveWithAnyArgs().Send(new ReadWeightSummariesQuery(UserId: default, DateFrom: default, DateTo: default, QuantizationDays: default), default);
        await waistReadService.DidNotReceiveWithAnyArgs().Send(new ReadWaistSummariesQuery(UserId: default, DateFrom: default, DateTo: default, QuantizationDays: default), default);
    }

    private static ICurrentUserAccessService CreateCurrentUserAccessService() {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(null));

        return service;
    }
}
