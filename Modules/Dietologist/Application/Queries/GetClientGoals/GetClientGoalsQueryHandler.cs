using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetClientGoals;

public sealed class GetClientGoalsQueryHandler(
    IDietologistInvitationReadModelRepository invitationRepository,
    IDietologistUserContextService dietologistUserContextService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetClientGoalsQuery, Result<ClientGoalsModel>> {
    public async Task<Result<ClientGoalsModel>> Handle(
        GetClientGoalsQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ClientGoalsModel>(userIdResult);
        }

        UserId dietologistUserId = userIdResult.Value;
        Guid clientUserId = query.ClientUserId;
        Result<string> dietologistResult = await dietologistUserContextService
            .GetAccessibleUserEmailAsync(dietologistUserId, cancellationToken)
            .ConfigureAwait(false);
        if (dietologistResult.IsFailure) {
            return Result.Failure<ClientGoalsModel>(dietologistResult.Error);
        }

        Result<UserId> clientResult = UserIdParser.Parse(
            clientUserId,
            Errors.Validation.Invalid(nameof(clientUserId), "Client user id must not be empty."));
        if (clientResult.IsFailure) {
            return UserIdParser.ToFailure<ClientGoalsModel>(clientResult);
        }

        Result<DietologistPermissionsModel> accessResult = await DietologistAccessPolicy.EnsureCanAccessClientReadModelAsync(
            invitationRepository, dietologistUserId, clientResult.Value, cancellationToken).ConfigureAwait(false);
        if (accessResult.IsFailure) {
            return Result.Failure<ClientGoalsModel>(accessResult.Error);
        }

        Error? permissionError = DietologistAccessPolicy.EnsurePermission(accessResult.Value, "Goals");
        if (permissionError is not null) {
            return Result.Failure<ClientGoalsModel>(permissionError);
        }

        Result<GoalsModel> goalsResult = await dietologistUserContextService
            .GetGoalsByIdAsync(clientResult.Value, cancellationToken)
            .ConfigureAwait(false);
        if (goalsResult.IsFailure) {
            return Result.Failure<ClientGoalsModel>(goalsResult.Error);
        }
        GoalsModel goals = goalsResult.Value;
        return Result.Success(new ClientGoalsModel(
            clientResult.Value.Value,
            goals.DailyCalorieTarget,
            goals.ProteinTarget,
            goals.FatTarget,
            goals.CarbTarget,
            goals.FiberTarget,
            goals.WaterGoal,
            goals.HydrationGoal,
            goals.DesiredWeightKg,
            goals.DesiredWaistCm,
            goals.StepGoal));
    }
}
