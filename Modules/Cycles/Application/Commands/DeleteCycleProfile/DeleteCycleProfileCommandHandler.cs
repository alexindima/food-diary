using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Cycles.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Cycles.Application.Abstractions.Common;
using FoodDiary.Modules.Cycles.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Cycles.Application.Internal;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Cycles.Application.Commands.DeleteCycleProfile;

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
            return Result.Failure(CycleErrors.NotFound(command.CycleProfileId));
        }

        await cycleRepository.DeleteAsync(profile, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
