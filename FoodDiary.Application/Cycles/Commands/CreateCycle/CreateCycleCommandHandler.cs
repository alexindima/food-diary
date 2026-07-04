using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Commands.CreateCycle;

public sealed class CreateCycleCommandHandler(
    ICycleWriteRepository cycleRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<CreateCycleCommand, Result<CycleModel>> {
    public async Task<Result<CycleModel>> Handle(
        CreateCycleCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<CycleModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<CycleModel>(accessError);
        }

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
                command.Notes));

            await cycleRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            CyclePredictionsModel existingPredictions = CyclePredictionService.CalculatePredictions(existing);
            return Result.Success(existing.ToModel(existingPredictions));
        }

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
            command.Notes);

        profile = await cycleRepository.AddAsync(profile, cancellationToken).ConfigureAwait(false);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile);
        return Result.Success(profile.ToModel(predictions));
    }
}
