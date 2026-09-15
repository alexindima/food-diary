using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Cycles.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Cycles.Domain.Entities;
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

namespace FoodDiary.Modules.Cycles.Application.Commands.ConfirmPeriodStart;

public sealed class ConfirmPeriodStartCommandHandler(
    ICycleWriteRepository cycleRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider? timeProvider = null)
    : ICommandHandler<ConfirmPeriodStartCommand, Result<CycleModel>> {
    public async Task<Result<CycleModel>> Handle(ConfirmPeriodStartCommand command, CancellationToken cancellationToken) {
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
            cancellationToken).ConfigureAwait(false);
        if (profile is null) {
            return Result.Failure<CycleModel>(CycleErrors.NotFound(command.CycleProfileId));
        }

        try {
            profile.ConfirmPeriodStart(command.Date);
        } catch (ArgumentException exception) {
            return Result.Failure<CycleModel>(Errors.Validation.Invalid(nameof(command.Date), exception.Message));
        }
        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, currentDate: currentDate, timeProvider: timeProvider);
        CyclePredictionRevisionService.Record(profile, predictions, timeProvider);
        await cycleRepository.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
        return Result.Success(profile.ToModel(predictions, currentDate));
    }
}
