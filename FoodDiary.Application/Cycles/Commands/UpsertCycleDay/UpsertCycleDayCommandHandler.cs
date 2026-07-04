using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Commands.UpsertCycleDay;

public class UpsertCycleDayCommandHandler(
    ICycleWriteRepository cycleRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<UpsertCycleDayCommand, Result<CycleLogDayModel>> {
    public async Task<Result<CycleLogDayModel>> Handle(
        UpsertCycleDayCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<CycleLogDayModel>(Errors.Authentication.InvalidToken);
        }

        if (command.CycleProfileId == Guid.Empty) {
            return Result.Failure<CycleLogDayModel>(
                Errors.Validation.Invalid(nameof(command.CycleProfileId), "Cycle profile id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<CycleLogDayModel>(accessError);
        }

        var profileId = new CycleProfileId(command.CycleProfileId);

        CycleProfile? profile = await cycleRepository.GetByIdAsync(
            profileId,
            userId,
            includeDetails: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (profile is null) {
            return Result.Failure<CycleLogDayModel>(Errors.Cycle.NotFound(command.CycleProfileId));
        }

        ApplyLog(profile, command);

        await cycleRepository.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
        return Result.Success(profile.ToDayModel(command.Date));
    }

    private static void ApplyLog(CycleProfile profile, UpsertCycleDayCommand command) {
        ApplyBleeding(profile, command);
        ApplySymptoms(profile, command);
        ApplyFertilitySignal(profile, command);
    }

    private static void ApplyBleeding(CycleProfile profile, UpsertCycleDayCommand command) {
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
