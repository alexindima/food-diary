using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;

public class UpdateWeightEntryCommandHandler(
    IWeightEntryRepository weightEntryRepository,
    ICurrentUserAccessService currentUserAccessService)
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
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<WeightEntryModel>(accessError);
        }

        var weightEntryId = new WeightEntryId(command.WeightEntryId);
        WeightEntry? existingEntry = await weightEntryRepository.GetByIdAsync(
            weightEntryId,
            userId,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (existingEntry is null) {
            return Result.Failure<WeightEntryModel>(Errors.WeightEntry.NotFound(command.WeightEntryId));
        }

        DateTime normalizedDate = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(command.Date);
        WeightEntry? duplicate = await weightEntryRepository.GetByDateAsync(
            userId,
            normalizedDate,
            cancellationToken).ConfigureAwait(false);

        if (duplicate is not null && duplicate.Id != existingEntry.Id) {
            return Result.Failure<WeightEntryModel>(
                Errors.WeightEntry.AlreadyExists(normalizedDate));
        }

        existingEntry.Update(command.Weight, normalizedDate);
        await weightEntryRepository.UpdateAsync(existingEntry, cancellationToken).ConfigureAwait(false);

        return Result.Success(existingEntry.ToModel());
    }
}
