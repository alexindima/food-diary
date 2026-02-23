using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;

public class CreateWaistEntryCommandHandler(IWaistEntryRepository waistEntryRepository)
    : ICommandHandler<CreateWaistEntryCommand, Result<WaistEntryResponse>> {
    public async Task<Result<WaistEntryResponse>> Handle(
        CreateWaistEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId.Value == UserId.Empty) {
            return Result.Failure<WaistEntryResponse>(Errors.Authentication.InvalidToken);
        }

        var normalizedDate = NormalizeUtcDate(command.Date);
        var existing = await waistEntryRepository.GetByDateAsync(
            command.UserId.Value,
            normalizedDate,
            cancellationToken);
        if (existing is not null) {
            return Result.Failure<WaistEntryResponse>(
                Errors.WaistEntry.AlreadyExists(normalizedDate));
        }

        var entry = WaistEntry.Create(command.UserId.Value, normalizedDate, command.Circumference);
        entry = await waistEntryRepository.AddAsync(entry, cancellationToken);
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
