using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;

public sealed class UpdateWeightEntryCommandHandler(
    IWeightEntryWriteRepository weightEntryRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<UpdateWeightEntryCommand, Result<WeightEntryModel>> {
    public async Task<Result<WeightEntryModel>> Handle(
        UpdateWeightEntryCommand command,
        CancellationToken cancellationToken) {
        Result<WeightEntryId> weightEntryIdResult = RequiredIdParser.Parse(
            command.WeightEntryId,
            nameof(command.WeightEntryId),
            "Weight entry id must not be empty.",
            value => new WeightEntryId(value));
        if (weightEntryIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<WeightEntryModel, WeightEntryId>(weightEntryIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<WeightEntryModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        WeightEntryId weightEntryId = weightEntryIdResult.Value;
        WeightEntry? existingEntry = await weightEntryRepository.GetByIdAsync(
            weightEntryId,
            userId,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (existingEntry is null) {
            return Result.Failure<WeightEntryModel>(Errors.WeightEntry.NotAccessible(command.WeightEntryId));
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
