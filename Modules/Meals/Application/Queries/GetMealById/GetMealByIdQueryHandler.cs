using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Meals.Common.Validation;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Meals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Meals.Queries.GetMealById;

public sealed class GetMealByIdQueryHandler(
    IMealReadService mealReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetMealByIdQuery, Result<MealModel>> {
    public async Task<Result<MealModel>> Handle(GetMealByIdQuery request, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            request.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<MealModel>(userIdResult);
        }

        Result<MealId> mealIdResult = RequiredIdParser.Parse(
            request.MealId,
            nameof(request.MealId),
            "Meal id must not be empty.",
            value => new MealId(value));
        if (mealIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<MealModel, MealId>(mealIdResult);
        }

        UserId userId = userIdResult.Value;
        MealId mealId = mealIdResult.Value;

        MealModel? meal = await mealReadService.GetByIdAsync(
            userId,
            mealId,
            cancellationToken).ConfigureAwait(false);

        return meal is null
            ? Result.Failure<MealModel>(Errors.Meal.NotFound(request.MealId))
            : Result.Success(meal);
    }
}
