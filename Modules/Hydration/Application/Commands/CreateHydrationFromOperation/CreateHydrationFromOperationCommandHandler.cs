using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Hydration.Validators;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Hydration.Commands.CreateHydrationFromOperation;

public sealed class CreateHydrationFromOperationCommandHandler(
    IHydrationEntryWriteRepository entries,
    IHydrationOperationReceiptRepository receipts,
    ICurrentUserAccessService currentUserAccessService) : ICommandHandler<CreateHydrationFromOperationCommand, Result<HydrationOperationModel>> {
    public async Task<Result<HydrationOperationModel>> Handle(CreateHydrationFromOperationCommand command, CancellationToken cancellationToken) {
        Result<UserId> owner = await CurrentUserAccessResolver.ResolveAsync(command.UserId, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (owner.IsFailure) {
            return Result.Failure<HydrationOperationModel>(owner.Error);
        }
        Result amount = HydrationValidators.ValidateAmount(command.AmountMl);
        if (amount.IsFailure) {
            return Result.Failure<HydrationOperationModel>(amount.Error);
        }
        if (command.OperationId == Guid.Empty || command.TimestampUtc.Kind != DateTimeKind.Utc) {
            return Result.Failure<HydrationOperationModel>(new Error("Hydration.InvalidOperation", "Operation ID and UTC timestamp are required.", ErrorKind.Validation));
        }
        HydrationOperationReceipt? existing = await receipts.FindAsync(owner.Value, command.OperationId, cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            return existing.OperationId == command.OperationId && existing.Matches(owner.Value, command.AmountMl, command.TimestampUtc)
                ? Result.Success(ToModel(existing))
                : Result.Failure<HydrationOperationModel>(new Error("Hydration.OperationConflict", "The operation was already saved with different input.", ErrorKind.Conflict));
        }
        var timestamp = new DateTime(command.TimestampUtc.Ticks / 10 * 10, DateTimeKind.Utc);
        var entry = HydrationEntry.Create(owner.Value, timestamp, command.AmountMl);
        var receipt = HydrationOperationReceipt.Create(command.OperationId, entry);
        // The shared command unit of work saves both records in one EF transaction.
        await entries.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        await receipts.AddAsync(receipt, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToModel(receipt));
    }

    private static HydrationOperationModel ToModel(HydrationOperationReceipt receipt) =>
        new(receipt.OperationId, receipt.EntryId.Value, receipt.TimestampUtc, receipt.AmountMl);
}
