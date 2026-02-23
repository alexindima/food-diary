using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Contracts.WeightEntries;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;

public class CreateWeightEntryCommandHandler(IWeightEntryRepository weightEntryRepository)
    : ICommandHandler<CreateWeightEntryCommand, Result<WeightEntryResponse>> {
    public async Task<Result<WeightEntryResponse>> Handle(
        CreateWeightEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId.Value == UserId.Empty) {
            return Result.Failure<WeightEntryResponse>(Errors.Authentication.InvalidToken);
        }

        var normalizedDate = NormalizeUtcDate(command.Date);
        var existing = await weightEntryRepository.GetByDateAsync(
            command.UserId.Value,
            normalizedDate,
            cancellationToken);
        if (existing is not null) {
            return Result.Failure<WeightEntryResponse>(
                Errors.WeightEntry.AlreadyExists(normalizedDate));
        }

        var entry = WeightEntry.Create(command.UserId.Value, normalizedDate, command.Weight);
        entry = await weightEntryRepository.AddAsync(entry, cancellationToken);

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
