using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;

public class UpdateWaistEntryCommandHandler(IWaistEntryRepository waistEntryRepository)
    : ICommandHandler<UpdateWaistEntryCommand, Result<WaistEntryModel>> {
    public async Task<Result<WaistEntryModel>> Handle(
        UpdateWaistEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId.Value == Guid.Empty) {
            return Result.Failure<WaistEntryModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var waistEntryId = new WaistEntryId(command.WaistEntryId);
        var entry = await waistEntryRepository.GetByIdAsync(
            waistEntryId,
            userId,
            asTracking: true,
            cancellationToken);

        if (entry is null) {
            return Result.Failure<WaistEntryModel>(Errors.WaistEntry.NotFound(command.WaistEntryId));
        }

        var normalizedDate = NormalizeUtcDate(command.Date);
        var existing = await waistEntryRepository.GetByDateAsync(
            userId,
            normalizedDate,
            cancellationToken);

        if (existing is not null && existing.Id != entry.Id) {
            return Result.Failure<WaistEntryModel>(
                Errors.WaistEntry.AlreadyExists(normalizedDate));
        }

        entry.Update(command.Circumference, normalizedDate);
        await waistEntryRepository.UpdateAsync(entry, cancellationToken);
        return Result.Success(entry.ToModel());
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
