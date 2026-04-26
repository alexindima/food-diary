using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Queries.GetMyDietologist;

public class GetMyDietologistQueryHandler(
    IDietologistInvitationRepository invitationRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetMyDietologistQuery, Result<DietologistInfoModel?>> {
    public async Task<Result<DietologistInfoModel?>> Handle(GetMyDietologistQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<DietologistInfoModel?>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<DietologistInfoModel?>(accessError);
        }

        var invitation = await invitationRepository.GetActiveByClientAsync(userId, cancellationToken: cancellationToken);
        return Result.Success(invitation?.ToDietologistInfoModel());
    }
}
