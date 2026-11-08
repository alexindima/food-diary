using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Commands.RecordUserActivity;

public sealed class RecordUserActivityCommandHandler(
    IUserActivityRepository activity,
    ICurrentUserAccessService users,
    TimeProvider clock) : ICommandHandler<RecordUserActivityCommand, Result> {
    public async Task<Result> Handle(RecordUserActivityCommand command, CancellationToken cancellationToken) {
        Result<UserId> userId = await CurrentUserAccessResolver.ResolveAsync(command.UserId, users, cancellationToken);
        if (userId.IsFailure) {
            return Result.Failure(userId.Error);
        }
        await activity.RecordAsync(userId.Value, clock.GetUtcNow().UtcDateTime, cancellationToken);
        return Result.Success();
    }
}
