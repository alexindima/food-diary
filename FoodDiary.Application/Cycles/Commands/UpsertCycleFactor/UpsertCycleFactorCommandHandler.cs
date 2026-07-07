using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Cycles.Commands.UpsertCycleFactor;

public sealed class UpsertCycleFactorCommandHandler(
    ICycleWriteRepository cycleRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<UpsertCycleFactorCommand, Result<CycleModel>> {
    public async Task<Result<CycleModel>> Handle(UpsertCycleFactorCommand command, CancellationToken cancellationToken) {
        if (command.CycleProfileId == Guid.Empty) {
            return Result.Failure<CycleModel>(
                Errors.Validation.Invalid(nameof(command.CycleProfileId), "Cycle profile id must not be empty."));
        }

        if (!Enum.IsDefined((CycleFactorType)command.Type)) {
            return Result.Failure<CycleModel>(
                Errors.Validation.Invalid(nameof(command.Type), "Cycle factor type is invalid."));
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<CycleModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        CycleProfile? profile = await cycleRepository.GetByIdAsync(
            new CycleProfileId(command.CycleProfileId),
            userId,
            includeDetails: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (profile is null) {
            return Result.Failure<CycleModel>(Errors.Cycle.NotFound(command.CycleProfileId));
        }

        profile.UpsertFactor((CycleFactorType)command.Type, command.StartDate, command.EndDate, command.Notes, command.ClearNotes);

        await cycleRepository.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile);
        return Result.Success(profile.ToModel(predictions));
    }
}
