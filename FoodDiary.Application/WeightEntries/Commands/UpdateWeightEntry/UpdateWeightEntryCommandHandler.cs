using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Contracts.WeightEntries;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;

public class UpdateWeightEntryCommandHandler(IWeightEntryRepository weightEntryRepository)
    : ICommandHandler<UpdateWeightEntryCommand, Result<WeightEntryResponse>> {
    public async Task<Result<WeightEntryResponse>> Handle(
        UpdateWeightEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId.Value == UserId.Empty) {
            return Result.Failure<WeightEntryResponse>(Errors.Authentication.InvalidToken);
        }

        var existingEntry = await weightEntryRepository.GetByIdAsync(
            command.WeightEntryId,
            command.UserId.Value,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (existingEntry is null) {
            return Result.Failure<WeightEntryResponse>(Errors.WeightEntry.NotFound(command.WeightEntryId.Value));
        }

        var normalizedDate = NormalizeUtcDate(command.Date);
        var duplicate = await weightEntryRepository.GetByDateAsync(
            command.UserId.Value,
            normalizedDate,
            cancellationToken);

        if (duplicate is not null && duplicate.Id != existingEntry.Id) {
            return Result.Failure<WeightEntryResponse>(
                Errors.WeightEntry.AlreadyExists(normalizedDate));
        }

        existingEntry.Update(command.Weight, normalizedDate);
        await weightEntryRepository.UpdateAsync(existingEntry, cancellationToken);

        return Result.Success(existingEntry.ToResponse());
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
