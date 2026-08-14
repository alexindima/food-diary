using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Application.BodyMetrics.WaistEntries.Queries.GetWaistHistoryPageSummary;
using FoodDiary.Application.BodyMetrics.WaistEntries.Models;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Application.BodyMetrics.WeightEntries.Queries.GetWeightHistoryPageSummary;
using FoodDiary.Application.BodyMetrics.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Users;

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

    [Fact]
    public async Task Handlers_WhenProfileReadFails_ReturnFailureWithoutReadingEntries() {
        IUserProfileReadService profiles = CreateProfiles(fail: true);
        IWeightEntryReadService weightEntries = Substitute.For<IWeightEntryReadService>();
        IWaistEntryReadService waistEntries = Substitute.For<IWaistEntryReadService>();

        Result<WeightHistoryPageSummaryModel> weight = await CreateWeightHandler(weightEntries, profiles)
            .Handle(new GetWeightHistoryPageSummaryQuery(UserId.Value, DateFrom, DateTo, 3, 500), CancellationToken.None);
        Result<WaistHistoryPageSummaryModel> waist = await CreateWaistHandler(waistEntries, profiles)
            .Handle(new GetWaistHistoryPageSummaryQuery(UserId.Value, DateFrom, DateTo, 3, 500), CancellationToken.None);

        ResultAssert.Failure(weight);
        ResultAssert.Failure(waist);
        await weightEntries.DidNotReceiveWithAnyArgs().GetEntriesAsync(
            userId: default,
            dateFrom: default,
            dateTo: default,
            limit: default,
            descending: default,
            cancellationToken: default);
        await waistEntries.DidNotReceiveWithAnyArgs().GetEntriesAsync(
            userId: default,
            dateFrom: default,
            dateTo: default,
            limit: default,
            descending: default,
            cancellationToken: default);
    }

    [Fact]
    public async Task Handlers_WithValidQuery_ReturnAggregatedPageSummaries() {
        IUserProfileReadService profiles = CreateProfiles(fail: false);
        IWeightEntryReadService weightEntries = Substitute.For<IWeightEntryReadService>();
        weightEntries.GetEntriesAsync(
                UserId,
                dateFrom: null,
                dateTo: null,
                limit: 25,
                descending: true,
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns([new WeightEntryModel(Guid.NewGuid(), UserId.Value, DateTo, 75)]);
        weightEntries.GetSummariesAsync(UserId, DateFrom, DateTo, 5, Arg.Any<CancellationToken>())
            .Returns([new WeightEntrySummaryModel(DateFrom, DateTo, 75)]);
        IWaistEntryReadService waistEntries = Substitute.For<IWaistEntryReadService>();
        waistEntries.GetEntriesAsync(
                UserId,
                dateFrom: null,
                dateTo: null,
                limit: 25,
                descending: true,
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns([new WaistEntryModel(Guid.NewGuid(), UserId.Value, DateTo, 80)]);
        waistEntries.GetSummariesAsync(UserId, DateFrom, DateTo, 5, Arg.Any<CancellationToken>())
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
            () => Assert.Equal(180, weight.Value.Height),
            () => Assert.Single(waist.Value.Entries),
            () => Assert.Single(waist.Value.Summary),
            () => Assert.Equal(180, waist.Value.Height));
    }

    private static GetWeightHistoryPageSummaryQueryHandler CreateWeightHandler(
        IWeightEntryReadService? entries = null,
        IUserProfileReadService? profiles = null) =>
        new(entries ?? Substitute.For<IWeightEntryReadService>(), profiles ?? CreateProfiles(fail: false), CreateAccess());

    private static GetWaistHistoryPageSummaryQueryHandler CreateWaistHandler(
        IWaistEntryReadService? entries = null,
        IUserProfileReadService? profiles = null) =>
        new(entries ?? Substitute.For<IWaistEntryReadService>(), profiles ?? CreateProfiles(fail: false), CreateAccess());

    private static ICurrentUserAccessService CreateAccess() {
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(UserId, Arg.Any<CancellationToken>()).Returns((Error?)null);
        return access;
    }

    private static IUserProfileReadService CreateProfiles(bool fail) {
        IUserProfileReadService profiles = Substitute.For<IUserProfileReadService>();
        Result<WeightHistoryProfileModel> weight = fail
            ? Result.Failure<WeightHistoryProfileModel>(Errors.Authentication.InvalidToken)
            : Result.Success(new WeightHistoryProfileModel(180, new UserDesiredWeightModel(72), []));
        Result<WaistHistoryProfileModel> waist = fail
            ? Result.Failure<WaistHistoryProfileModel>(Errors.Authentication.InvalidToken)
            : Result.Success(new WaistHistoryProfileModel(180, new UserDesiredWaistModel(76), []));
        profiles.GetWeightHistoryProfileAsync(UserId, Arg.Any<CancellationToken>()).Returns(weight);
        profiles.GetWaistHistoryProfileAsync(UserId, Arg.Any<CancellationToken>()).Returns(waist);
        return profiles;
    }
}
