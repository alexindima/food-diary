using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Cycles.Internal;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Application.Cycles.Services;

namespace FoodDiary.Application.Cycles.Commands.UpsertCycleDay;

public sealed class UpsertCycleDayCommandHandler(
    ICycleWriteRepository cycleRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider? timeProvider = null)
    : ICommandHandler<UpsertCycleDayCommand, Result<CycleLogDayModel>> {
    public async Task<Result<CycleLogDayModel>> Handle(
        UpsertCycleDayCommand command,
        CancellationToken cancellationToken) {
        Result<CycleProfileId> profileIdResult = RequiredIdParser.Parse(
            command.CycleProfileId,
            nameof(command.CycleProfileId),
            "Cycle profile id must not be empty.",
            value => new CycleProfileId(value));
        if (profileIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<CycleLogDayModel, CycleProfileId>(profileIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<CycleLogDayModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        CycleProfileId profileId = profileIdResult.Value;

        CycleProfile? profile = await cycleRepository.GetByIdAsync(
            profileId,
            userId,
            includeDetails: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (profile is null) {
            return Result.Failure<CycleLogDayModel>(Errors.Cycle.NotFound(command.CycleProfileId));
        }

        if (command.FertilitySignal is not null &&
            !profile.HasActiveConsent(CycleConsentPurpose.FertilitySignals)) {
            return Result.Failure<CycleLogDayModel>(Errors.Validation.Invalid(
                nameof(command.FertilitySignal),
                "Active fertility consent is required."));
        }

        ApplyLog(profile, command);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, timeProvider: timeProvider);
        CyclePredictionRevisionService.Record(profile, predictions, timeProvider);

        await cycleRepository.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
        return Result.Success(profile.ToDayModel(command.Date));
    }

    private static void ApplyLog(CycleProfile profile, UpsertCycleDayCommand command) {
        ApplyBleeding(profile, command);
        ApplySymptoms(profile, command);
        ApplyFertilitySignal(profile, command);
    }

    private static void ApplyBleeding(CycleProfile profile, UpsertCycleDayCommand command) {
        if (command.ClearBleeding) {
            profile.ClearBleedingEntries(command.Date);
        }

        if (command.Bleeding is null) {
            return;
        }

        profile.UpsertBleedingEntry(
            command.Date,
            (BleedingType)command.Bleeding.Type,
            (CycleFlowLevel)command.Bleeding.Flow,
            command.Bleeding.PainImpact,
            command.Bleeding.Notes,
            command.Bleeding.ClearNotes);
    }

    private static void ApplySymptoms(CycleProfile profile, UpsertCycleDayCommand command) {
        profile.ClearSymptomEntries(
            command.Date,
            (command.ClearSymptomCategories ?? []).Select(static category => (CycleSymptomCategory)category).ToHashSet());

        foreach (SymptomLogCommandModel symptom in command.Symptoms) {
            profile.UpsertSymptomEntry(
                command.Date,
                (CycleSymptomCategory)symptom.Category,
                symptom.Intensity,
                symptom.Tags,
                symptom.Note,
                symptom.ClearNote);
        }
    }

    private static void ApplyFertilitySignal(CycleProfile profile, UpsertCycleDayCommand command) {
        if (command.ClearFertilitySignal) {
            profile.ClearFertilitySignal(command.Date);
        }

        if (command.FertilitySignal is null) {
            return;
        }

        profile.UpsertFertilitySignal(
            command.Date,
            command.FertilitySignal.BasalBodyTemperatureCelsius,
            command.FertilitySignal.OvulationTestResult.HasValue ? (OvulationTestResult)command.FertilitySignal.OvulationTestResult.Value : null,
            command.FertilitySignal.CervicalFluid,
            command.FertilitySignal.HadSex,
            command.FertilitySignal.Notes,
            command.FertilitySignal.ClearNotes);
    }
}
