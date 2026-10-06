using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.Common.Validation;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.DeleteMealPlan;

public sealed class DeleteMealPlanCommandHandler(
    IMealPlanWriteRepository repository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteMealPlanCommand, Result> {
    public async Task<Result> Handle(DeleteMealPlanCommand command, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        Result<MealPlanId> planId = RequiredIdParser.Parse(
            command.PlanId, nameof(command.PlanId), "Meal plan ID is required.", value => new MealPlanId(value));
        if (planId.IsFailure) {
            return Result.Failure(MealPlanErrors.InvalidId);
        }

        Result<UserId> access = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (access.IsFailure) {
            return Result.Failure(access.Error);
        }

        bool removed = await repository.DeletePersonalAsync(
            planId.Value, access.Value, cancellationToken).ConfigureAwait(false);
        return removed ? Result.Success() : Result.Failure(MealPlanErrors.NotFound(command.PlanId));
    }
}
