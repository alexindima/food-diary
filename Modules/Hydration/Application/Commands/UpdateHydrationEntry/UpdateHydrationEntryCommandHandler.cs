using FoodDiary.Modules.Hydration.Application.Mappings;
using FoodDiary.Modules.Hydration.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Hydration.Application.Internal;
using FoodDiary.Modules.Hydration.Application.Abstractions.Common;

using FoodDiary.Modules.Hydration.Contracts.Models;
using FoodDiary.Modules.Hydration.Application.Validators;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Hydration.Application.Commands.UpdateHydrationEntry;

public sealed class UpdateHydrationEntryCommandHandler(
    IHydrationEntryWriteRepository repository,
    ICurrentUserAccessService currentUserAccessService) : ICommandHandler<UpdateHydrationEntryCommand, Result<HydrationEntryModel>> {
    public async Task<Result<HydrationEntryModel>> Handle(
        UpdateHydrationEntryCommand command,
        CancellationToken cancellationToken) {
        Result<HydrationEntryId> hydrationEntryIdResult = RequiredIdParser.Parse(
            command.HydrationEntryId,
            nameof(command.HydrationEntryId),
            "Hydration entry id must not be empty.",
            value => new HydrationEntryId(value));
        if (hydrationEntryIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<HydrationEntryModel, HydrationEntryId>(hydrationEntryIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<HydrationEntryModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        HydrationEntryId hydrationEntryId = hydrationEntryIdResult.Value;

        HydrationEntry? entry = await repository.GetByIdForUpdateAsync(
            hydrationEntryId,
            cancellationToken).ConfigureAwait(false);
        if (entry is null || entry.UserId != userId) {
            return Result.Failure<HydrationEntryModel>(HydrationEntryErrors.NotAccessible(command.HydrationEntryId));
        }

        if (command.AmountMl.HasValue) {
            Result validation = HydrationValidators.ValidateAmount(command.AmountMl.Value);
            if (validation.IsFailure) {
                return Result.Failure<HydrationEntryModel>(validation.Error);
            }
        }

        DateTime? timestampUtc = command.TimestampUtc.HasValue
            ? UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(command.TimestampUtc.Value)
            : null;
        entry.Update(command.AmountMl, timestampUtc);
        await repository.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);

        return Result.Success(entry.ToModel());
    }
}
