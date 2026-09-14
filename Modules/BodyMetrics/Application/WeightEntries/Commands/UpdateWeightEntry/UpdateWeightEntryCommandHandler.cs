using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Mappings;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.BodyMetrics.Application.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Commands.UpdateWeightEntry;

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
            "WeightKg entry id must not be empty.",
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
            return Result.Failure<WeightEntryModel>(WeightEntryErrors.NotAccessible(command.WeightEntryId));
        }

        DateTime normalizedDate = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(command.Date);
        WeightEntry? duplicate = await weightEntryRepository.GetByDateAsync(
            userId,
            normalizedDate,
            cancellationToken).ConfigureAwait(false);

        if (duplicate is not null && duplicate.Id != existingEntry.Id) {
            return Result.Failure<WeightEntryModel>(
                WeightEntryErrors.AlreadyExists(normalizedDate));
        }

        existingEntry.Update(command.WeightKg, normalizedDate);
        await weightEntryRepository.UpdateAsync(existingEntry, cancellationToken).ConfigureAwait(false);

        return Result.Success(existingEntry.ToModel());
    }
}
