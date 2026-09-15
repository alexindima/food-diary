using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Modules.Hydration.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Hydration.Application.Commands.CreateHydrationFromOperation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Hydration.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class CreateHydrationFromOperationTests {
    [Theory]
    [InlineData("access")]
    [InlineData("amount")]
    [InlineData("operation")]
    [InlineData("timestamp")]
    public async Task InvalidInput_DoesNotReadOrWriteReceipts(string scenario) {
        var owner = new UserId(Guid.NewGuid());
        IHydrationEntryWriteRepository entries = Substitute.For<IHydrationEntryWriteRepository>();
        IHydrationOperationReceiptRepository receipts = Substitute.For<IHydrationOperationReceiptRepository>();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        if (string.Equals(scenario, "access", StringComparison.Ordinal)) {
            access.EnsureCanAccessAsync(owner, Arg.Any<CancellationToken>()).Returns(new FoodDiary.Results.Error("User.Denied", "Denied"));
        }
        Guid operation = string.Equals(scenario, "operation", StringComparison.Ordinal) ? Guid.Empty : Guid.NewGuid();
        DateTime timestamp = string.Equals(scenario, "timestamp", StringComparison.Ordinal) ? DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified) : DateTime.UtcNow;
        int amount = string.Equals(scenario, "amount", StringComparison.Ordinal) ? 0 : 250;

        FoodDiary.Results.Result<FoodDiary.Modules.Hydration.Application.Models.HydrationOperationModel> result = await new CreateHydrationFromOperationCommandHandler(entries, receipts, access)
            .Handle(new CreateHydrationFromOperationCommand(owner.Value, operation, timestamp, amount), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(receipts.ReceivedCalls());
        Assert.Empty(entries.ReceivedCalls());
    }

    [Fact]
    public async Task NewOperation_StagesWaterAndReceiptTogether() {
        var owner = new UserId(Guid.NewGuid());
        var operationId = Guid.NewGuid();
        DateTime timestamp = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);
        IHydrationEntryWriteRepository entries = Substitute.For<IHydrationEntryWriteRepository>();
        IHydrationOperationReceiptRepository receipts = Substitute.For<IHydrationOperationReceiptRepository>();
        var handler = new CreateHydrationFromOperationCommandHandler(entries, receipts, Substitute.For<ICurrentUserAccessService>());

        FoodDiary.Results.Result<FoodDiary.Modules.Hydration.Application.Models.HydrationOperationModel> result = await handler.Handle(new CreateHydrationFromOperationCommand(owner.Value, operationId, timestamp, 250), CancellationToken.None);

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

        FoodDiary.Results.Result<FoodDiary.Modules.Hydration.Application.Models.HydrationOperationModel> result = await handler.Handle(new CreateHydrationFromOperationCommand(owner.Value, operationId, timestamp, amount), CancellationToken.None);

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
