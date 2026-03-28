using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;

public class CreateWaistEntryCommandHandler(IWaistEntryRepository waistEntryRepository)
    : ICommandHandler<CreateWaistEntryCommand, Result<WaistEntryModel>> {
    public async Task<Result<WaistEntryModel>> Handle(
        CreateWaistEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<WaistEntryModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var normalizedDate = NormalizeUtcDate(command.Date);
        var existing = await waistEntryRepository.GetByDateAsync(
            userId,
            normalizedDate,
            cancellationToken);
        if (existing is not null) {
            return Result.Failure<WaistEntryModel>(
                Errors.WaistEntry.AlreadyExists(normalizedDate));
        }

        var entry = WaistEntry.Create(userId, normalizedDate, command.Circumference);
        entry = await waistEntryRepository.AddAsync(entry, cancellationToken);
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
