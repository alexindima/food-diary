using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Meals.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Meals.Commands.DeleteMeal;

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
            return Result.Failure(Errors.Meal.NotFound(command.MealId));
        }

        await mealWriteRepository.DeleteAsync(meal, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
