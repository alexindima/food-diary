using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Commands.CreateWeightEntry;
using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Commands.UpdateWeightEntry;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Commands.CreateWaistEntry;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Commands.UpdateWaistEntry;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.BodyMetrics.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class MeasurementDayCommandTests {
    private static readonly DateTime LeapDay = new(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public async Task Create_NormalizesTheSameDayForLookupMutationAndResponse(DateTimeKind kind) {
        var userId = UserId.New();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        IWeightEntryWriteRepository weights = Substitute.For<IWeightEntryWriteRepository>();
        IWaistEntryWriteRepository waists = Substitute.For<IWaistEntryWriteRepository>();
        weights.AddAsync(Arg.Any<WeightEntry>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<WeightEntry>()));
        waists.AddAsync(Arg.Any<WaistEntry>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<WaistEntry>()));
        DateTime utc = LeapDay.AddHours(23).AddMinutes(59);
        DateTime encoded = kind == DateTimeKind.Local ? utc.ToLocalTime() : DateTime.SpecifyKind(utc, kind);
        using var cancellation = new CancellationTokenSource();

        WeightEntryModel weight = ResultAssert.Success(await new CreateWeightEntryCommandHandler(weights, access).Handle(
            new CreateWeightEntryCommand(userId.Value, encoded, 72.125), cancellation.Token));
        WaistEntryModel waist = ResultAssert.Success(await new CreateWaistEntryCommandHandler(waists, access).Handle(
            new CreateWaistEntryCommand(userId.Value, encoded, 85.125), cancellation.Token));

        Assert.Multiple(() => {
            Assert.Equal(LeapDay, weight.Date);
            Assert.Equal(LeapDay, waist.Date);
            Assert.Equal(DateTimeKind.Utc, weight.Date.Kind);
            Assert.Equal(DateTimeKind.Utc, waist.Date.Kind);
            Assert.Equal(72.125, weight.WeightKg);
            Assert.Equal(85.125, waist.CircumferenceCm);
        });
        await weights.Received(1).GetByDateAsync(userId, LeapDay, cancellation.Token);
        await waists.Received(1).GetByDateAsync(userId, LeapDay, cancellation.Token);
        await access.Received(2).EnsureCanAccessAsync(userId, cancellation.Token);
    }

    [Fact]
    public async Task Update_UsesTheSelectedLeapDayAndKeepsOwnerScopedLookup() {
        var userId = UserId.New();
        var previousDay = new MeasurementDay(new DateOnly(2024, 2, 28));
        var weight = WeightEntry.CreateForDay(userId, previousDay, 72);
        var waist = WaistEntry.CreateForDay(userId, previousDay, 85);
        IWeightEntryWriteRepository weights = Substitute.For<IWeightEntryWriteRepository>();
        IWaistEntryWriteRepository waists = Substitute.For<IWaistEntryWriteRepository>();
        weights.GetByIdAsync(weight.Id, userId, asTracking: true, Arg.Any<CancellationToken>()).Returns(weight);
        waists.GetByIdAsync(waist.Id, userId, asTracking: true, Arg.Any<CancellationToken>()).Returns(waist);
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        var encoded = DateTime.SpecifyKind(LeapDay.AddHours(23), DateTimeKind.Unspecified);

        WeightEntryModel weightResult = ResultAssert.Success(await new UpdateWeightEntryCommandHandler(weights, access).Handle(
            new UpdateWeightEntryCommand(userId.Value, weight.Id.Value, encoded, 73.125), CancellationToken.None));
        WaistEntryModel waistResult = ResultAssert.Success(await new UpdateWaistEntryCommandHandler(waists, access).Handle(
            new UpdateWaistEntryCommand(userId.Value, waist.Id.Value, encoded, 86.125), CancellationToken.None));

        Assert.Multiple(() => {
            Assert.Equal(weight.Id.Value, weightResult.Id);
            Assert.Equal(waist.Id.Value, waistResult.Id);
            Assert.Equal(LeapDay, weightResult.Date);
            Assert.Equal(LeapDay, waistResult.Date);
            Assert.Equal(73.125, weightResult.WeightKg);
            Assert.Equal(86.125, waistResult.CircumferenceCm);
        });
        await weights.Received(1).GetByDateAsync(userId, LeapDay, CancellationToken.None);
        await waists.Received(1).GetByDateAsync(userId, LeapDay, CancellationToken.None);
        await weights.Received(1).UpdateAsync(weight, CancellationToken.None);
        await waists.Received(1).UpdateAsync(waist, CancellationToken.None);
    }

    [Fact]
    public async Task DuplicateDay_PreservesWeightConflictAndWaistReplaySuccess() {
        var userId = UserId.New();
        var day = new MeasurementDay(new DateOnly(2024, 2, 29));
        var weight = WeightEntry.CreateForDay(userId, day, 72);
        var waist = WaistEntry.CreateForDay(userId, day, 85);
        IWeightEntryWriteRepository weights = Substitute.For<IWeightEntryWriteRepository>();
        IWaistEntryWriteRepository waists = Substitute.For<IWaistEntryWriteRepository>();
        weights.GetByDateAsync(userId, LeapDay, Arg.Any<CancellationToken>()).Returns(weight);
        waists.GetByDateAsync(userId, LeapDay, Arg.Any<CancellationToken>()).Returns(waist);
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();

        ResultAssert.Failure(await new CreateWeightEntryCommandHandler(weights, access).Handle(
            new CreateWeightEntryCommand(userId.Value, LeapDay.AddHours(23), 72), CancellationToken.None),
            WeightEntryErrors.AlreadyExists(LeapDay).Code);
        WaistEntryModel replay = ResultAssert.Success(await new CreateWaistEntryCommandHandler(waists, access).Handle(
            new CreateWaistEntryCommand(userId.Value, LeapDay.AddHours(23), 85), CancellationToken.None));

        Assert.Equal(waist.Id.Value, replay.Id);
        await weights.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await waists.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
