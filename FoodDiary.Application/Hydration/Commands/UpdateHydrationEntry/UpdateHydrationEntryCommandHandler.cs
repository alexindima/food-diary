using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Hydration.Mappings;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Hydration.Validators;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;

public class UpdateHydrationEntryCommandHandler(
    IHydrationEntryRepository repository,
    IUserRepository userRepository) : ICommandHandler<UpdateHydrationEntryCommand, Result<HydrationEntryModel>> {
    public async Task<Result<HydrationEntryModel>> Handle(
        UpdateHydrationEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<HydrationEntryModel>(Errors.Authentication.InvalidToken);
        }

        if (command.HydrationEntryId == Guid.Empty) {
            return Result.Failure<HydrationEntryModel>(
                Errors.Validation.Invalid(nameof(command.HydrationEntryId), "Hydration entry id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<HydrationEntryModel>(accessError);
        }

        var hydrationEntryId = new HydrationEntryId(command.HydrationEntryId);

        var entry = await repository.GetByIdAsync(
            hydrationEntryId,
            asTracking: true,
            cancellationToken: cancellationToken);
        if (entry is null || entry.UserId != userId) {
            return Result.Failure<HydrationEntryModel>(Errors.HydrationEntry.NotFound(command.HydrationEntryId));
        }

        if (command.AmountMl.HasValue) {
            var validation = HydrationValidators.ValidateAmount(command.AmountMl.Value);
            if (validation.IsFailure) {
                return Result.Failure<HydrationEntryModel>(validation.Error);
            }
        }

        var timestampUtc = command.TimestampUtc.HasValue
            ? UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(command.TimestampUtc.Value)
            : (DateTime?)null;
        entry.Update(command.AmountMl, timestampUtc);
        await repository.UpdateAsync(entry, cancellationToken);

        return Result.Success(entry.ToModel());
    }
}
