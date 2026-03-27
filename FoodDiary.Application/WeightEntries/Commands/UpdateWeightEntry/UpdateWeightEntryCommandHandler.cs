using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;

public class UpdateWeightEntryCommandHandler(IWeightEntryRepository weightEntryRepository)
    : ICommandHandler<UpdateWeightEntryCommand, Result<WeightEntryModel>> {
    public async Task<Result<WeightEntryModel>> Handle(
        UpdateWeightEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId.Value == Guid.Empty) {
            return Result.Failure<WeightEntryModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var weightEntryId = new WeightEntryId(command.WeightEntryId);
        var existingEntry = await weightEntryRepository.GetByIdAsync(
            weightEntryId,
            userId,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (existingEntry is null) {
            return Result.Failure<WeightEntryModel>(Errors.WeightEntry.NotFound(command.WeightEntryId));
        }

        var normalizedDate = NormalizeUtcDate(command.Date);
        var duplicate = await weightEntryRepository.GetByDateAsync(
            userId,
            normalizedDate,
            cancellationToken);

        if (duplicate is not null && duplicate.Id != existingEntry.Id) {
            return Result.Failure<WeightEntryModel>(
                Errors.WeightEntry.AlreadyExists(normalizedDate));
        }

        existingEntry.Update(command.Weight, normalizedDate);
        await weightEntryRepository.UpdateAsync(existingEntry, cancellationToken);

        return Result.Success(existingEntry.ToModel());
    }

    private static DateTime NormalizeUtcDate(DateTime value) {
        var utc = value.Kind switch {
            DateTimeKind.Utc => value,
            _ => value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
