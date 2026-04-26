using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;

public class UpdateWeightEntryCommandHandler(
    IWeightEntryRepository weightEntryRepository,
    IUserRepository userRepository)
    : ICommandHandler<UpdateWeightEntryCommand, Result<WeightEntryModel>> {
    public async Task<Result<WeightEntryModel>> Handle(
        UpdateWeightEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<WeightEntryModel>(Errors.Authentication.InvalidToken);
        }

        if (command.WeightEntryId == Guid.Empty) {
            return Result.Failure<WeightEntryModel>(
                Errors.Validation.Invalid(nameof(command.WeightEntryId), "Weight entry id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<WeightEntryModel>(accessError);
        }

        var weightEntryId = new WeightEntryId(command.WeightEntryId);
        var existingEntry = await weightEntryRepository.GetByIdAsync(
            weightEntryId,
            userId,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (existingEntry is null) {
            return Result.Failure<WeightEntryModel>(Errors.WeightEntry.NotFound(command.WeightEntryId));
        }

        var normalizedDate = UtcDateNormalizer.NormalizeDateUsingLocalFallback(command.Date);
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
}
