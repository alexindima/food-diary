using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Contracts.Ai;

namespace FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;

public sealed class CalculateFoodNutritionCommandHandler(IOpenAiFoodService openAiFoodService)
    : IQueryHandler<CalculateFoodNutritionCommand, Result<FoodNutritionResponse>>
{
    public async Task<Result<FoodNutritionResponse>> Handle(
        CalculateFoodNutritionCommand query,
        CancellationToken cancellationToken)
    {
        if (query.Items.Count == 0)
        {
            return Result.Failure<FoodNutritionResponse>(Errors.Ai.EmptyItems());
        }

        return await openAiFoodService.CalculateNutritionAsync(query.Items, cancellationToken);
    }
}
