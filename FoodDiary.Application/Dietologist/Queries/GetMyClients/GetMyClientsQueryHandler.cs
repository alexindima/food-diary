using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetMyClients;

public class GetMyClientsQueryHandler(
    IDietologistInvitationRepository invitationRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetMyClientsQuery, Result<IReadOnlyList<ClientSummaryModel>>> {
    public async Task<Result<IReadOnlyList<ClientSummaryModel>>> Handle(GetMyClientsQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<ClientSummaryModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<ClientSummaryModel>>(accessError);
        }

        var invitations = await invitationRepository.GetActiveByDietologistAsync(userId, cancellationToken);
        var clients = invitations.Select(i => i.ToClientSummaryModel()).ToList();
        return Result.Success<IReadOnlyList<ClientSummaryModel>>(clients);
    }
}
