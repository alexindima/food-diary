using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Queries.GetMyClientTasks;

public sealed class GetMyClientTasksQueryHandler(
    IClientTaskRepository taskRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider timeProvider)
    : IQueryHandler<GetMyClientTasksQuery, Result<IReadOnlyList<ClientTaskModel>>> {
    public async Task<Result<IReadOnlyList<ClientTaskModel>>> Handle(
        GetMyClientTasksQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId, currentUserAccessService, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<ClientTaskModel>>(userIdResult);
        }

        IReadOnlyList<ClientTaskReadModel> tasks = await taskRepository.GetByClientAsync(
            userIdResult.Value, cancellationToken).ConfigureAwait(false);
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        return Result.Success<IReadOnlyList<ClientTaskModel>>(
            tasks.Select(task => task.ToModel(utcNow)).ToList());
    }
}
