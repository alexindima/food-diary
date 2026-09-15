using FoodDiary.Modules.Cycles.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Cycles.Application.Abstractions.Common;
using FoodDiary.Modules.Cycles.Contracts.Common;
using FoodDiary.Modules.Cycles.Application.Internal;
using FoodDiary.Modules.Cycles.Application.Mappings;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Modules.Cycles.Application.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Application.Commands.UpsertCycleFactor;

public sealed class UpsertCycleFactorCommandHandler(
    ICycleWriteRepository cycleRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider? timeProvider = null)
    : ICommandHandler<UpsertCycleFactorCommand, Result<CycleModel>> {
    public async Task<Result<CycleModel>> Handle(UpsertCycleFactorCommand command, CancellationToken cancellationToken) {
        var currentDate = DateOnly.FromDateTime((timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime);
        Result<CycleProfileId> profileIdResult = RequiredIdParser.Parse(
            command.CycleProfileId,
            nameof(command.CycleProfileId),
            "Cycle profile id must not be empty.",
            value => new CycleProfileId(value));
        if (profileIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<CycleModel, CycleProfileId>(profileIdResult);
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
            profileIdResult.Value,
            userId,
            includeDetails: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (profile is null) {
            return Result.Failure<CycleModel>(CycleErrors.NotFound(command.CycleProfileId));
        }

        profile.UpsertFactor((CycleFactorType)command.Type, command.StartDate, command.EndDate, command.Notes, command.ClearNotes);

        await cycleRepository.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, currentDate: currentDate, timeProvider: timeProvider);
        CyclePredictionRevisionService.Record(profile, predictions, timeProvider);
        return Result.Success(profile.ToModel(predictions, currentDate));
    }
}
