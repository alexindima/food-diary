using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistEntries;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.GetWaistHistoryPageSummary;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.GetWeightHistoryPageSummary;
using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.BodyMetrics.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class HistoryPageSummaryHandlerTests {
    private static readonly UserId UserId = UserId.New();
    private static readonly DateTime DateFrom = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DateTo = DateFrom.AddDays(30);

    [Fact]
    public async Task Handlers_WithInvalidCurrentUser_ReturnAccessFailures() {
        Result<WeightHistoryPageSummaryModel> weight = await CreateWeightHandler()
            .Handle(new GetWeightHistoryPageSummaryQuery(Guid.Empty, DateFrom, DateTo, 3, 500), CancellationToken.None);
        Result<WaistHistoryPageSummaryModel> waist = await CreateWaistHandler()
            .Handle(new GetWaistHistoryPageSummaryQuery(Guid.Empty, DateFrom, DateTo, 3, 500), CancellationToken.None);

        ResultAssert.Failure(weight);
        ResultAssert.Failure(waist);
    }

    [Theory]
    [InlineData(1, 3, 500)]
    [InlineData(0, 0, 500)]
    [InlineData(0, 3, 0)]
    [InlineData(0, 3, 501)]
    public async Task Handlers_WithInvalidQuery_ReturnValidationFailures(int reversedRange, int quantizationDays, int entriesLimit) {
        DateTime from = reversedRange == 1 ? DateTo : DateFrom;
        DateTime to = reversedRange == 1 ? DateFrom : DateTo;

        Result<WeightHistoryPageSummaryModel> weight = await CreateWeightHandler()
            .Handle(new GetWeightHistoryPageSummaryQuery(UserId.Value, from, to, quantizationDays, entriesLimit), CancellationToken.None);
        Result<WaistHistoryPageSummaryModel> waist = await CreateWaistHandler()
            .Handle(new GetWaistHistoryPageSummaryQuery(UserId.Value, from, to, quantizationDays, entriesLimit), CancellationToken.None);

        ResultAssert.Failure(weight);
        ResultAssert.Failure(waist);
        Assert.Equal("Validation.Invalid", weight.Error.Code);
        Assert.Equal("Validation.Invalid", waist.Error.Code);
    }

    [Theory]
    [InlineData(366, 3)]
    [InlineData(30, 367)]
    [InlineData(30, int.MaxValue)]
    public async Task Handlers_WithUnsupportedTemporalRange_ReturnValidationFailures(int periodDays, int quantizationDays) {
        Result<WeightHistoryPageSummaryModel> weight = await CreateWeightHandler()
            .Handle(
                new GetWeightHistoryPageSummaryQuery(
                    UserId.Value,
                    DateFrom,
                    DateFrom.AddDays(periodDays),
                    quantizationDays,
                    500),
                CancellationToken.None);
        Result<WaistHistoryPageSummaryModel> waist = await CreateWaistHandler()
            .Handle(
                new GetWaistHistoryPageSummaryQuery(
                    UserId.Value,
                    DateFrom,
                    DateFrom.AddDays(periodDays),
                    quantizationDays,
                    500),
                CancellationToken.None);

        ResultAssert.Failure(weight);
        ResultAssert.Failure(waist);
        Assert.Equal("Validation.Invalid", weight.Error.Code);
        Assert.Equal("Validation.Invalid", waist.Error.Code);
    }

    [Fact]
    public async Task Handlers_WhenProfileReadFails_ReturnFailureWithoutReadingEntries() {
        IUserBodyMetricHistoryReadService profiles = CreateProfiles(fail: true);
        ISender weightEntries = Substitute.For<ISender>();
        ISender waistEntries = Substitute.For<ISender>();

        Result<WeightHistoryPageSummaryModel> weight = await CreateWeightHandler(weightEntries, profiles)
            .Handle(new GetWeightHistoryPageSummaryQuery(UserId.Value, DateFrom, DateTo, 3, 500), CancellationToken.None);
        Result<WaistHistoryPageSummaryModel> waist = await CreateWaistHandler(waistEntries, profiles)
            .Handle(new GetWaistHistoryPageSummaryQuery(UserId.Value, DateFrom, DateTo, 3, 500), CancellationToken.None);

        ResultAssert.Failure(weight);
        ResultAssert.Failure(waist);
        await weightEntries.DidNotReceiveWithAnyArgs().Send(new ReadWeightEntriesQuery(UserId: default, DateFrom: default, DateTo: default, Limit: default, Descending: default), default);
        await waistEntries.DidNotReceiveWithAnyArgs().Send(new ReadWaistEntriesQuery(UserId: default, DateFrom: default, DateTo: default, Limit: default, Descending: default), default);
    }

    [Fact]
    public async Task Handlers_WithValidQuery_ReturnAggregatedPageSummaries() {
        IUserBodyMetricHistoryReadService profiles = CreateProfiles(fail: false);
        ISender weightEntries = Substitute.For<ISender>();
        weightEntries.Send(new ReadWeightEntriesQuery(UserId: UserId, DateFrom: null, DateTo: null, Limit: 25, Descending: true), Arg.Any<CancellationToken>())
            .Returns([new WeightEntryModel(Guid.NewGuid(), UserId.Value, DateTo, 75)]);
        weightEntries.Send(new ReadWeightSummariesQuery(UserId: UserId, DateFrom: DateFrom, DateTo: DateTo, QuantizationDays: 5), Arg.Any<CancellationToken>())
            .Returns([new WeightEntrySummaryModel(DateFrom, DateTo, 75)]);
        ISender waistEntries = Substitute.For<ISender>();
        waistEntries.Send(new ReadWaistEntriesQuery(UserId: UserId, DateFrom: null, DateTo: null, Limit: 25, Descending: true), Arg.Any<CancellationToken>())
            .Returns([new WaistEntryModel(Guid.NewGuid(), UserId.Value, DateTo, 80)]);
        waistEntries.Send(new ReadWaistSummariesQuery(UserId: UserId, DateFrom: DateFrom, DateTo: DateTo, QuantizationDays: 5), Arg.Any<CancellationToken>())
            .Returns([new WaistEntrySummaryModel(DateFrom, DateTo, 80)]);

        Result<WeightHistoryPageSummaryModel> weight = await CreateWeightHandler(weightEntries, profiles)
            .Handle(new GetWeightHistoryPageSummaryQuery(UserId.Value, DateFrom, DateTo, 5, 25), CancellationToken.None);
        Result<WaistHistoryPageSummaryModel> waist = await CreateWaistHandler(waistEntries, profiles)
            .Handle(new GetWaistHistoryPageSummaryQuery(UserId.Value, DateFrom, DateTo, 5, 25), CancellationToken.None);

        ResultAssert.Success(weight);
        ResultAssert.Success(waist);
        Assert.Multiple(
            () => Assert.Single(weight.Value.Entries),
            () => Assert.Single(weight.Value.Summary),
            () => Assert.Equal(180, weight.Value.HeightCm),
            () => Assert.Single(waist.Value.Entries),
            () => Assert.Single(waist.Value.Summary),
            () => Assert.Equal(180, waist.Value.HeightCm));
    }

    private static GetWeightHistoryPageSummaryQueryHandler CreateWeightHandler(
        ISender? entries = null,
        IUserBodyMetricHistoryReadService? profiles = null) =>
        new(entries ?? Substitute.For<ISender>(), profiles ?? CreateProfiles(fail: false), CreateAccess());

    private static GetWaistHistoryPageSummaryQueryHandler CreateWaistHandler(
        ISender? entries = null,
        IUserBodyMetricHistoryReadService? profiles = null) =>
        new(entries ?? Substitute.For<ISender>(), profiles ?? CreateProfiles(fail: false), CreateAccess());

    private static ICurrentUserAccessService CreateAccess() {
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(UserId, Arg.Any<CancellationToken>()).Returns((Error?)null);
        return access;
    }

    private static IUserBodyMetricHistoryReadService CreateProfiles(bool fail) {
        IUserBodyMetricHistoryReadService profiles = Substitute.For<IUserBodyMetricHistoryReadService>();
        Result<WeightHistoryProfileModel> weight = fail
            ? Result.Failure<WeightHistoryProfileModel>(AuthenticationErrors.InvalidToken)
            : Result.Success(new WeightHistoryProfileModel(180, new UserDesiredWeightModel(72), []));
        Result<WaistHistoryProfileModel> waist = fail
            ? Result.Failure<WaistHistoryProfileModel>(AuthenticationErrors.InvalidToken)
            : Result.Success(new WaistHistoryProfileModel(180, new UserDesiredWaistModel(76), []));
        profiles.GetWeightHistoryProfileAsync(UserId, Arg.Any<CancellationToken>()).Returns(weight);
        profiles.GetWaistHistoryProfileAsync(UserId, Arg.Any<CancellationToken>()).Returns(waist);
        return profiles;
    }
}
