using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Queries.GetClientTasksForDietologist;

public sealed class GetClientTasksForDietologistQueryHandler(
    IClientTaskRepository taskRepository,
    ICurrentUserAccessService currentUserAccessService,
    TimeProvider timeProvider)
    : IQueryHandler<GetClientTasksForDietologistQuery, Result<IReadOnlyList<ClientTaskModel>>> {
    public async Task<Result<IReadOnlyList<ClientTaskModel>>> Handle(
        GetClientTasksForDietologistQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> dietologistIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId, currentUserAccessService, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (dietologistIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<ClientTaskModel>>(dietologistIdResult);
        }

        Result<UserId> clientIdResult = RequiredIdParser.Parse(
            query.ClientUserId,
            nameof(query.ClientUserId),
            "Client user id must not be empty.",
            value => new UserId(value));
        if (clientIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<ClientTaskModel>>(clientIdResult.Error);
        }

        IReadOnlyList<ClientTaskReadModel> tasks = await taskRepository.GetByDietologistAndClientAsync(
            dietologistIdResult.Value,
            clientIdResult.Value,
            cancellationToken).ConfigureAwait(false);
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        return Result.Success<IReadOnlyList<ClientTaskModel>>(
            tasks.Select(task => task.ToModel(utcNow)).ToList());
    }
}
