using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Dietologist.Queries.GetClientGoals;

public class GetClientGoalsQueryHandler(
    IDietologistInvitationRepository invitationRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetClientGoalsQuery, Result<UserModel>> {
    public async Task<Result<UserModel>> Handle(
        GetClientGoalsQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<UserModel>(Errors.Authentication.InvalidToken);
        }

        var dietologistUserId = new UserId(query.UserId!.Value);
        Error? currentUserAccessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(
            userRepository, dietologistUserId, cancellationToken).ConfigureAwait(false);
        if (currentUserAccessError is not null) {
            return Result.Failure<UserModel>(currentUserAccessError);
        }

        var clientUserId = new UserId(query.ClientUserId);

        Result<DietologistPermissionsModel> accessResult = await DietologistAccessPolicy.EnsureCanAccessClientAsync(
            invitationRepository, dietologistUserId, clientUserId, cancellationToken).ConfigureAwait(false);

        if (accessResult.IsFailure) {
            return Result.Failure<UserModel>(accessResult.Error);
        }

        Error? permissionError = DietologistAccessPolicy.EnsurePermission(accessResult.Value, "Goals");
        if (permissionError is not null) {
            return Result.Failure<UserModel>(permissionError);
        }

        User? user = await userRepository.GetByIdAsync(clientUserId, cancellationToken).ConfigureAwait(false);
        return user is null ? Result.Failure<UserModel>(Errors.Dietologist.AccessDenied) : Result.Success(user.ToModel());
    }
}
