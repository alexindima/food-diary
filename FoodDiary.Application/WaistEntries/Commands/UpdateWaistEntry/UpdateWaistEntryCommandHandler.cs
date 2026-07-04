using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;

public class UpdateWaistEntryCommandHandler(
    IWaistEntryWriteRepository waistEntryRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<UpdateWaistEntryCommand, Result<WaistEntryModel>> {
    public async Task<Result<WaistEntryModel>> Handle(
        UpdateWaistEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<WaistEntryModel>(Errors.Authentication.InvalidToken);
        }

        if (command.WaistEntryId == Guid.Empty) {
            return Result.Failure<WaistEntryModel>(
                Errors.Validation.Invalid(nameof(command.WaistEntryId), "Waist entry id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<WaistEntryModel>(accessError);
        }

        var waistEntryId = new WaistEntryId(command.WaistEntryId);
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
