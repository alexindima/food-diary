using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Mappings;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.BodyMetrics.Application.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;

namespace FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Commands.CreateWaistEntry;

public sealed class CreateWaistEntryCommandHandler(
    IWaistEntryWriteRepository waistEntryRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<CreateWaistEntryCommand, Result<WaistEntryModel>> {
    public async Task<Result<WaistEntryModel>> Handle(
        CreateWaistEntryCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<WaistEntryModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        DateTime normalizedDate = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(command.Date);
        WaistEntry? existing = await waistEntryRepository.GetByDateAsync(
            userId,
            normalizedDate,
            cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            if (existing.CircumferenceCm.Equals(command.CircumferenceCm)) {
                return Result.Success(existing.ToModel());
            }

            return Result.Failure<WaistEntryModel>(
                WaistEntryErrors.AlreadyExists(normalizedDate));
        }

        var entry = WaistEntry.Create(userId, normalizedDate, command.CircumferenceCm);
        entry = await waistEntryRepository.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success(entry.ToModel());
    }
}
