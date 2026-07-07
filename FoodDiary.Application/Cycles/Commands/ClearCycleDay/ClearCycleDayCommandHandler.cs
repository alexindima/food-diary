using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Cycles.Commands.ClearCycleDay;

public sealed class ClearCycleDayCommandHandler(
    ICycleWriteRepository cycleRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<ClearCycleDayCommand, Result> {
    public async Task<Result> Handle(ClearCycleDayCommand command, CancellationToken cancellationToken) {
        if (command.CycleProfileId == Guid.Empty) {
            return Result.Failure(
                Errors.Validation.Invalid(nameof(command.CycleProfileId), "Cycle profile id must not be empty."));
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        var profileId = new CycleProfileId(command.CycleProfileId);
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
            await cycleRepository.UpdateAsync(profile, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
