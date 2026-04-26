using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetMyDietologistRelationship;

public class GetMyDietologistRelationshipQueryHandler(
    IDietologistInvitationRepository invitationRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetMyDietologistRelationshipQuery, Result<DietologistRelationshipModel?>> {
    public async Task<Result<DietologistRelationshipModel?>> Handle(
        GetMyDietologistRelationshipQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<DietologistRelationshipModel?>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<DietologistRelationshipModel?>(accessError);
        }

        var accepted = await invitationRepository.GetActiveByClientAsync(userId, cancellationToken: cancellationToken);
        if (accepted is not null) {
            return Result.Success<DietologistRelationshipModel?>(accepted.ToRelationshipModel());
        }

        var pending = await invitationRepository.GetByClientAndStatusAsync(
            userId,
            DietologistInvitationStatus.Pending,
            cancellationToken: cancellationToken);

        return Result.Success<DietologistRelationshipModel?>(pending?.ToRelationshipModel());
    }
}
