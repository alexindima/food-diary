using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Validation;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetMyClientTasks;

public sealed class GetMyClientTasksQueryHandler(
    IClientTaskReadModelRepository taskRepository,
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

        int page = PaginationPolicy.NormalizePage(query.Page);
        int limit = PaginationPolicy.NormalizePageSize(query.Limit);
        IReadOnlyList<ClientTaskReadModel> tasks = await taskRepository.GetByClientAsync(
            userIdResult.Value, page, limit, cancellationToken).ConfigureAwait(false);
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        return Result.Success<IReadOnlyList<ClientTaskModel>>(
            tasks.Select(task => task.ToModel(utcNow)).ToList());
    }
}
