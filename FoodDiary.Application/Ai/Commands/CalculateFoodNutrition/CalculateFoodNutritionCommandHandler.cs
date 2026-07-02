using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;

public sealed class CalculateFoodNutritionCommandHandler(
    IOpenAiFoodService openAiFoodService,
    IAiUserContextService aiUserContextService)
    : ICommandHandler<CalculateFoodNutritionCommand, Result<FoodNutritionModel>> {
    public async Task<Result<FoodNutritionModel>> Handle(
        CalculateFoodNutritionCommand query,
        CancellationToken cancellationToken) {
        if (query.UserId == Guid.Empty) {
            return Result.Failure<FoodNutritionModel>(
                Errors.Validation.Invalid(nameof(query.UserId), "User id must not be empty."));
        }

        if (query.Items.Count == 0) {
            return Result.Failure<FoodNutritionModel>(Errors.Ai.EmptyItems());
        }

        var userId = new UserId(query.UserId);
        Result<AiUserContext> contextResult = await aiUserContextService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<FoodNutritionModel>(contextResult.Error);
        }

        return await openAiFoodService.CalculateNutritionAsync(query.Items, userId, cancellationToken).ConfigureAwait(false);
    }
}
