using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;

public class CreateWeightEntryCommandHandler(IWeightEntryRepository weightEntryRepository)
    : ICommandHandler<CreateWeightEntryCommand, Result<WeightEntryModel>> {
    public async Task<Result<WeightEntryModel>> Handle(
        CreateWeightEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId.Value == Guid.Empty) {
            return Result.Failure<WeightEntryModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var normalizedDate = NormalizeUtcDate(command.Date);
        var existing = await weightEntryRepository.GetByDateAsync(
            userId,
            normalizedDate,
            cancellationToken);
        if (existing is not null) {
            return Result.Failure<WeightEntryModel>(
                Errors.WeightEntry.AlreadyExists(normalizedDate));
        }

        var entry = WeightEntry.Create(userId, normalizedDate, command.Weight);
        entry = await weightEntryRepository.AddAsync(entry, cancellationToken);

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
