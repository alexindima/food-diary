using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Services;

public sealed class DietologistClientReadService(
    IDietologistInvitationReadModelRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService)
    : IDietologistClientReadService {
    public async Task<Result<UserModel>> GetGoalsAsync(
        UserId dietologistUserId,
        Guid clientUserId,
        CancellationToken cancellationToken) {
        Result<string> dietologistResult = await dietologistUserContextService
            .GetAccessibleUserEmailAsync(dietologistUserId, cancellationToken)
            .ConfigureAwait(false);
        if (dietologistResult.IsFailure) {
            return Result.Failure<UserModel>(dietologistResult.Error);
        }

        Result<UserId> clientResult = UserIdParser.Parse(
            clientUserId,
            Errors.Validation.Invalid(nameof(clientUserId), "Client user id must not be empty."));
        if (clientResult.IsFailure) {
            return UserIdParser.ToFailure<UserModel>(clientResult);
        }

        Result<DietologistPermissionsModel> accessResult = await DietologistAccessPolicy.EnsureCanAccessClientReadModelAsync(
            invitationRepository, dietologistUserId, clientResult.Value, cancellationToken).ConfigureAwait(false);
        if (accessResult.IsFailure) {
            return Result.Failure<UserModel>(accessResult.Error);
        }

        Error? permissionError = DietologistAccessPolicy.EnsurePermission(accessResult.Value, "Goals");
        if (permissionError is not null) {
            return Result.Failure<UserModel>(permissionError);
        }

        return await dietologistUserContextService
            .GetUserModelByIdAsync(clientResult.Value, cancellationToken)
            .ConfigureAwait(false);
    }
}
