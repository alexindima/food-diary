using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Ai.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;

public sealed class CalculateFoodNutritionCommandHandler(IOpenAiFoodService openAiFoodService)
    : IQueryHandler<CalculateFoodNutritionCommand, Result<FoodNutritionModel>> {
    public async Task<Result<FoodNutritionModel>> Handle(
        CalculateFoodNutritionCommand query,
        CancellationToken cancellationToken) {
        if (query.Items.Count == 0) {
            return Result.Failure<FoodNutritionModel>(Errors.Ai.EmptyItems());
        }

        return await openAiFoodService.CalculateNutritionAsync(query.Items, new UserId(query.UserId), cancellationToken);
    }
}
