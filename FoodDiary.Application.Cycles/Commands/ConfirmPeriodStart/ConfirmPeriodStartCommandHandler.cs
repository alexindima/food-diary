using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Cycles.Internal;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Cycles.Commands.ConfirmPeriodStart;

public sealed class ConfirmPeriodStartCommandHandler(
    ICycleWriteRepository cycleRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<ConfirmPeriodStartCommand, Result<CycleModel>> {
    public async Task<Result<CycleModel>> Handle(ConfirmPeriodStartCommand command, CancellationToken cancellationToken) {
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
            cancellationToken).ConfigureAwait(false);
        if (profile is null) {
            return Result.Failure<CycleModel>(Errors.Cycle.NotFound(command.CycleProfileId));
        }

        profile.ConfirmPeriodStart(command.Date);
        await cycleRepository.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
        return Result.Success(profile.ToModel(CyclePredictionService.CalculatePredictions(profile)));
    }
}
