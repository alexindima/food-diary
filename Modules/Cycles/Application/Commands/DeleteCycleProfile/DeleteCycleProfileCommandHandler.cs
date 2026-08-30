using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Cycles.Internal;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Cycles.Commands.DeleteCycleProfile;

public sealed class DeleteCycleProfileCommandHandler(
    ICycleWriteRepository cycleRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteCycleProfileCommand, Result> {
    public async Task<Result> Handle(DeleteCycleProfileCommand command, CancellationToken cancellationToken) {
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

        CycleProfile? profile = await cycleRepository.GetByIdAsync(
            profileIdResult.Value,
            userIdResult.Value,
            includeDetails: false,
            asTracking: true,
            cancellationToken).ConfigureAwait(false);
        if (profile is null) {
            return Result.Failure(Errors.Cycle.NotFound(command.CycleProfileId));
        }

        await cycleRepository.DeleteAsync(profile, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
