using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Cycles.Internal;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Services;

namespace FoodDiary.Application.Cycles.Commands.ClearCycleDay;

public sealed class ClearCycleDayCommandHandler(
    ICycleWriteRepository cycleRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider? timeProvider = null)
    : ICommandHandler<ClearCycleDayCommand, Result> {
    public async Task<Result> Handle(ClearCycleDayCommand command, CancellationToken cancellationToken) {
        Result<CycleProfileId> profileIdResult = RequiredIdParser.Parse(
            command.CycleProfileId,
            nameof(command.CycleProfileId),
            "Cycle profile id must not be empty.",
            value => new CycleProfileId(value));
        if (profileIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(profileIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
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
            return Result.Failure(Errors.Cycle.NotFound(command.CycleProfileId));
        }

        if (profile.ClearDay(command.Date)) {
            CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, timeProvider: timeProvider);
            CyclePredictionRevisionService.Record(profile, predictions, timeProvider);
            await cycleRepository.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
