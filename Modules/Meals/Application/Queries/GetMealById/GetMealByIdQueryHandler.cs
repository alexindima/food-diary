using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Modules.Meals.Service.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Application.Mappings;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Meals.Application.Common.Validation;

namespace FoodDiary.Modules.Meals.Application.Queries.GetMealById;

public sealed class GetMealByIdQueryHandler(
    IMealProjectionReadRepository mealRepository,
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

        MealModel? meal = await GetByIdAsync(
            userId,
            mealId,
            cancellationToken).ConfigureAwait(false);

        return meal is null
            ? Result.Failure<MealModel>(MealErrors.NotFound(request.MealId))
            : Result.Success(meal);
    }
    private async Task<MealModel?> GetByIdAsync(
        UserId userId,
        MealId mealId,
        CancellationToken cancellationToken) {
        MealProjectionReadModel? meal = await mealRepository.GetByIdMealProjectionAsync(
            mealId,
            userId,
            cancellationToken).ConfigureAwait(false);

        return meal?.ToModel();
    }
}
