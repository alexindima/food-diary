using FoodDiary.Modules.Dietologist.Application.Common.Validation;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetClientTasksForDietologist;

public sealed class GetClientTasksForDietologistQueryHandler(
    IClientTaskReadModelRepository taskRepository,
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

        Result<UserId> clientIdResult = DietologistRequiredIdParser.Parse(
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
