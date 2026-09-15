using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Modules.Cycles.Application.Abstractions.Common;
using FoodDiary.Modules.Cycles.Application.Mappings;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Modules.Cycles.Application.Services;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Application.Commands.CreateCycle;

public sealed class CreateCycleCommandHandler(
    ICycleWriteRepository cycleRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider timeProvider)
    : ICommandHandler<CreateCycleCommand, Result<CycleModel>> {
    public CreateCycleCommandHandler(
        ICycleWriteRepository cycleRepository,
        ICurrentUserAccessService currentUserAccessService)
        : this(cycleRepository, currentUserAccessService, TimeProvider.System) {
    }

    public async Task<Result<CycleModel>> Handle(
        CreateCycleCommand command,
        CancellationToken cancellationToken) {
        var currentDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (command.CycleTrackingConsentGranted is not true) {
            return Result.Failure<CycleModel>(Errors.Validation.Invalid(
                nameof(command.CycleTrackingConsentGranted),
                "Explicit consent is required to enable cycle tracking."));
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<CycleModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        CycleProfile? existing = await cycleRepository.GetCurrentAsync(
            userId,
            includeDetails: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (existing is not null) {
            existing.UpdateSettings(new CycleProfileSettings(
                (CycleTrackingMode)command.Mode,
                command.AverageCycleLength,
                command.AveragePeriodLength,
                command.LutealLength,
                command.IsRegular,
                command.IsOnboardingComplete,
                command.ShowFertilityEstimates,
                command.DiscreetNotifications,
                command.Notes,
                Goal: command.Goal.HasValue ? (CycleTrackingGoal)command.Goal.Value : null,
                ReproductiveState: command.ReproductiveState.HasValue ? (CycleReproductiveState)command.ReproductiveState.Value : null,
                HideFromDashboard: command.HideFromDashboard));

            DateTime nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            existing.GrantConsent(CycleConsentPurpose.CycleTracking, nowUtc);
            ApplyOptionalConsents(existing, command, nowUtc);

            await cycleRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            CyclePredictionsModel existingPredictions = CyclePredictionService.CalculatePredictions(
                existing,
                currentDate: currentDate, timeProvider: timeProvider);
            CyclePredictionRevisionService.Record(existing, existingPredictions, timeProvider);
            return Result.Success(existing.ToModel(existingPredictions, currentDate));
        }

        DateTime createdAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var profile = CycleProfile.Create(
            userId,
            command.TrackingStartDate,
            (CycleTrackingMode)command.Mode,
            command.AverageCycleLength,
            command.AveragePeriodLength,
            command.LutealLength,
            command.IsRegular,
            command.IsOnboardingComplete,
            command.ShowFertilityEstimates,
            command.DiscreetNotifications,
            command.Notes,
            command.Goal.HasValue ? (CycleTrackingGoal)command.Goal.Value : null,
            command.ReproductiveState.HasValue ? (CycleReproductiveState)command.ReproductiveState.Value : null,
            command.HideFromDashboard,
            createdAtUtc);

        ApplyOptionalConsents(profile, command, createdAtUtc);

        profile = await cycleRepository.AddAsync(profile, cancellationToken).ConfigureAwait(false);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(
            profile,
            currentDate: currentDate, timeProvider: timeProvider);
        CyclePredictionRevisionService.Record(profile, predictions, timeProvider);
        return Result.Success(profile.ToModel(predictions, currentDate));
    }

    private static void ApplyOptionalConsents(CycleProfile profile, CreateCycleCommand command, DateTime nowUtc) {
        if (command.NutritionInsightsConsentGranted) {
            profile.GrantConsent(CycleConsentPurpose.NutritionInsights, nowUtc);
        }

        if (command.FertilitySignalsConsentGranted) {
            profile.GrantConsent(CycleConsentPurpose.FertilitySignals, nowUtc);
        }
    }
}
