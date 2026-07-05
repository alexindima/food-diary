using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Queries.GetClientGoals;

public sealed class GetClientGoalsQueryHandler(
    IDietologistInvitationReadRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService)
    : IQueryHandler<GetClientGoalsQuery, Result<UserModel>> {
    public async Task<Result<UserModel>> Handle(
        GetClientGoalsQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<UserModel>(Errors.Authentication.InvalidToken);
        }

        var dietologistUserId = new UserId(query.UserId!.Value);
        Result<string> dietologistResult = await dietologistUserContextService
            .GetAccessibleUserEmailAsync(dietologistUserId, cancellationToken)
            .ConfigureAwait(false);
        if (dietologistResult.IsFailure) {
            return Result.Failure<UserModel>(dietologistResult.Error);
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

        return await dietologistUserContextService.GetUserModelByIdAsync(clientUserId, cancellationToken).ConfigureAwait(false);
    }
}
