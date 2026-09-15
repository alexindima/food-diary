using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Meals.Application.Common.Validation;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Domain.Entities;

namespace FoodDiary.Modules.Meals.Application.Commands.DeleteMeal;

public sealed class DeleteMealCommandHandler(
    IMealReadRepository mealReadRepository,
    IMealWriteRepository mealWriteRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteMealCommand, Result> {
    public async Task<Result> Handle(DeleteMealCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        Result<MealId> mealIdResult = RequiredIdParser.Parse(
            command.MealId,
            nameof(command.MealId),
            "Meal id must not be empty.",
            value => new MealId(value));
        if (mealIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(mealIdResult);
        }

        UserId userId = userIdResult.Value;
        MealId mealId = mealIdResult.Value;

        Meal? meal = await mealReadRepository.GetByIdAsync(
            mealId,
            userId,
            includeItems: false,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (meal is null) {
            return Result.Failure(MealErrors.NotFound(command.MealId));
        }

        await mealWriteRepository.DeleteAsync(meal, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
