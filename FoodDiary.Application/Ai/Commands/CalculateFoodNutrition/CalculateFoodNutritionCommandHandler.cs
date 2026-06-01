using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;

public sealed class CalculateFoodNutritionCommandHandler(
    IOpenAiFoodService openAiFoodService,
    IUserRepository userRepository)
    : IQueryHandler<CalculateFoodNutritionCommand, Result<FoodNutritionModel>> {
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
        var user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<FoodNutritionModel>(accessError);
        }

        return await openAiFoodService.CalculateNutritionAsync(query.Items, userId, cancellationToken).ConfigureAwait(false);
    }
}
