using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Queries.GetMyClients;

public class GetMyClientsQueryHandler(
    IDietologistInvitationReadRepository invitationRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetMyClientsQuery, Result<IReadOnlyList<ClientSummaryModel>>> {
    public async Task<Result<IReadOnlyList<ClientSummaryModel>>> Handle(GetMyClientsQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<ClientSummaryModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<ClientSummaryModel>>(accessError);
        }

        IReadOnlyList<DietologistInvitation> invitations = await invitationRepository.GetActiveByDietologistAsync(userId, cancellationToken).ConfigureAwait(false);
        var clients = invitations.Select(i => i.ToClientSummaryModel()).ToList();
        return Result.Success<IReadOnlyList<ClientSummaryModel>>(clients);
    }
}
