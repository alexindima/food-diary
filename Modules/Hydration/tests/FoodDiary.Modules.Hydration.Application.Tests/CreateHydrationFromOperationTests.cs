using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Hydration.Commands.CreateHydrationFromOperation;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class CreateHydrationFromOperationTests {
    [Fact]
    public async Task NewOperation_StagesWaterAndReceiptTogether() {
        var owner = new UserId(Guid.NewGuid());
        var operationId = Guid.NewGuid();
        DateTime timestamp = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);
        IHydrationEntryWriteRepository entries = Substitute.For<IHydrationEntryWriteRepository>();
        IHydrationOperationReceiptRepository receipts = Substitute.For<IHydrationOperationReceiptRepository>();
        var handler = new CreateHydrationFromOperationCommandHandler(entries, receipts, Substitute.For<ICurrentUserAccessService>());

        FoodDiary.Results.Result<FoodDiary.Application.Hydration.Models.HydrationOperationModel> result = await handler.Handle(new CreateHydrationFromOperationCommand(owner.Value, operationId, timestamp, 250), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await entries.Received(1).AddAsync(Arg.Is<HydrationEntry>(entry => entry.Id.Value == result.Value.EntryId && entry.AmountMl == 250 && entry.Timestamp == timestamp), Arg.Any<CancellationToken>());
        await receipts.Received(1).AddAsync(Arg.Is<HydrationOperationReceipt>(receipt => receipt.EntryId.Value == result.Value.EntryId && receipt.OperationId == operationId), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(250, true)]
    [InlineData(500, false)]
    public async Task Replay_ReturnsOriginalEntryOrConflictWithoutAddingWater(int amount, bool matches) {
        var owner = new UserId(Guid.NewGuid());
        var operationId = Guid.NewGuid();
        DateTime timestamp = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);
        var entry = HydrationEntry.Create(owner, timestamp, 250);
        var receipt = HydrationOperationReceipt.Create(operationId, entry);
        IHydrationEntryWriteRepository entries = Substitute.For<IHydrationEntryWriteRepository>();
        IHydrationOperationReceiptRepository receipts = Substitute.For<IHydrationOperationReceiptRepository>();
        receipts.FindAsync(owner, operationId, Arg.Any<CancellationToken>()).Returns(receipt);
        var handler = new CreateHydrationFromOperationCommandHandler(entries, receipts, Substitute.For<ICurrentUserAccessService>());

        FoodDiary.Results.Result<FoodDiary.Application.Hydration.Models.HydrationOperationModel> result = await handler.Handle(new CreateHydrationFromOperationCommand(owner.Value, operationId, timestamp, amount), CancellationToken.None);

        Assert.Equal(matches, result.IsSuccess);
        if (matches) {
            Assert.Equal(entry.Id.Value, result.Value.EntryId);
        } else {
            Assert.Equal("Hydration.OperationConflict", result.Error.Code);
        }
        await entries.DidNotReceive().AddAsync(Arg.Any<HydrationEntry>(), Arg.Any<CancellationToken>());
        await receipts.DidNotReceive().AddAsync(Arg.Any<HydrationOperationReceipt>(), Arg.Any<CancellationToken>());
    }
}
