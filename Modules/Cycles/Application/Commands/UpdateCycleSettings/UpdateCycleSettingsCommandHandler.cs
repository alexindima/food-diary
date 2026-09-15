using FoodDiary.Modules.Cycles.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Cycles.Application.Abstractions.Common;
using FoodDiary.Modules.Cycles.Contracts.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Cycles.Application.Internal;
using FoodDiary.Modules.Cycles.Application.Mappings;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Modules.Cycles.Application.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Cycles.Application.Commands.UpdateCycleSettings;

public sealed class UpdateCycleSettingsCommandHandler(
    ICycleWriteRepository cycleRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider? timeProvider = null)
    : ICommandHandler<UpdateCycleSettingsCommand, Result<CycleModel>> {
    public async Task<Result<CycleModel>> Handle(UpdateCycleSettingsCommand command, CancellationToken cancellationToken) {
        var currentDate = DateOnly.FromDateTime((timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime);
        Result<CycleProfileId> profileIdResult = RequiredIdParser.Parse(
            command.CycleProfileId,
            nameof(command.CycleProfileId),
            "Cycle profile id must not be empty.",
            value => new CycleProfileId(value));
        if (profileIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<CycleModel, CycleProfileId>(profileIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<CycleModel>(userIdResult);
        }

        CycleProfile? profile = await cycleRepository.GetByIdAsync(
            profileIdResult.Value,
            userIdResult.Value,
            includeDetails: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (profile is null) {
            return Result.Failure<CycleModel>(CycleErrors.NotFound(command.CycleProfileId));
        }

        profile.UpdateSettings(new CycleProfileSettings(
            (CycleTrackingMode)command.Mode,
            command.AverageCycleLength,
            command.AveragePeriodLength,
            command.LutealLength,
            command.IsRegular,
            IsOnboardingComplete: null,
            command.ShowFertilityEstimates,
            command.DiscreetNotifications,
            Notes: null,
            Goal: command.Goal.HasValue ? (CycleTrackingGoal)command.Goal.Value : null,
            ReproductiveState: command.ReproductiveState.HasValue ? (CycleReproductiveState)command.ReproductiveState.Value : null,
            HideFromDashboard: command.HideFromDashboard));

        await cycleRepository.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, currentDate: currentDate, timeProvider: timeProvider);
        CyclePredictionRevisionService.Record(profile, predictions, timeProvider);
        return Result.Success(profile.ToModel(predictions, currentDate));
    }
}
