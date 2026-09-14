using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Application.Commands.CalculateFoodNutrition;

public sealed class CalculateFoodNutritionCommandHandler(
    IOpenAiFoodService openAiFoodService,
    IUserAiProfileReadService userProfileReadService)
    : ICommandHandler<CalculateFoodNutritionCommand, Result<FoodNutritionModel>> {
    public async Task<Result<FoodNutritionModel>> Handle(
        CalculateFoodNutritionCommand query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            query.UserId,
            Errors.Validation.Invalid(nameof(query.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<FoodNutritionModel>(userIdResult);
        }

        if (query.Items.Count == 0) {
            return Result.Failure<FoodNutritionModel>(AiErrors.EmptyItems());
        }

        UserId userId = userIdResult.Value;
        Result<UserAiProfileModel> contextResult = await userProfileReadService.GetAiProfileAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<FoodNutritionModel>(contextResult.Error);
        }

        return await openAiFoodService.CalculateNutritionAsync(query.Items, userId, query.RequestId, cancellationToken).ConfigureAwait(false);
    }
}
