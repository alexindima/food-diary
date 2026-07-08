using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;

public sealed class UpdateWaistEntryCommandHandler(
    IWaistEntryWriteRepository waistEntryRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<UpdateWaistEntryCommand, Result<WaistEntryModel>> {
    public async Task<Result<WaistEntryModel>> Handle(
        UpdateWaistEntryCommand command,
        CancellationToken cancellationToken) {
        Result<WaistEntryId> waistEntryIdResult = RequiredIdParser.Parse(
            command.WaistEntryId,
            nameof(command.WaistEntryId),
            "Waist entry id must not be empty.",
            value => new WaistEntryId(value));
        if (waistEntryIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<WaistEntryModel, WaistEntryId>(waistEntryIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<WaistEntryModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        WaistEntryId waistEntryId = waistEntryIdResult.Value;
        WaistEntry? entry = await waistEntryRepository.GetByIdAsync(
            waistEntryId,
            userId,
            asTracking: true,
            cancellationToken).ConfigureAwait(false);

        if (entry is null) {
            return Result.Failure<WaistEntryModel>(Errors.WaistEntry.NotFound(command.WaistEntryId));
        }

        DateTime normalizedDate = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(command.Date);
        WaistEntry? existing = await waistEntryRepository.GetByDateAsync(
            userId,
            normalizedDate,
            cancellationToken).ConfigureAwait(false);

        if (existing is not null && existing.Id != entry.Id) {
            return Result.Failure<WaistEntryModel>(
                Errors.WaistEntry.AlreadyExists(normalizedDate));
        }

        entry.Update(command.Circumference, normalizedDate);
        await waistEntryRepository.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success(entry.ToModel());
    }
}
