using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;

public class UpdateWaistEntryCommandHandler(IWaistEntryRepository waistEntryRepository)
    : ICommandHandler<UpdateWaistEntryCommand, Result<WaistEntryResponse>> {
    public async Task<Result<WaistEntryResponse>> Handle(
        UpdateWaistEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId.Value == UserId.Empty) {
            return Result.Failure<WaistEntryResponse>(Errors.Authentication.InvalidToken);
        }

        var entry = await waistEntryRepository.GetByIdAsync(
            command.WaistEntryId,
            command.UserId.Value,
            asTracking: true,
            cancellationToken);

        if (entry is null) {
            return Result.Failure<WaistEntryResponse>(Errors.WaistEntry.NotFound(command.WaistEntryId.Value));
        }

        var normalizedDate = NormalizeUtcDate(command.Date);
        var existing = await waistEntryRepository.GetByDateAsync(
            command.UserId.Value,
            normalizedDate,
            cancellationToken);

        if (existing is not null && existing.Id != entry.Id) {
            return Result.Failure<WaistEntryResponse>(
                Errors.WaistEntry.AlreadyExists(normalizedDate));
        }

        entry.Update(command.Circumference, normalizedDate);
        await waistEntryRepository.UpdateAsync(entry, cancellationToken);
        return Result.Success(entry.ToResponse());
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
