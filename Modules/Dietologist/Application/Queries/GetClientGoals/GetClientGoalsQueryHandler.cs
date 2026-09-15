using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetClientGoals;

public sealed class GetClientGoalsQueryHandler(
    IDietologistInvitationReadModelRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetClientGoalsQuery, Result<UserModel>> {
    public async Task<Result<UserModel>> Handle(
        GetClientGoalsQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<UserModel>(userIdResult);
        }

        UserId dietologistUserId = userIdResult.Value;
        Guid clientUserId = query.ClientUserId;
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
