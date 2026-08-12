using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Usda.Commands.UnlinkProductFromUsdaFood;

public record UnlinkProductFromUsdaFoodCommand(
    Guid? UserId,
    Guid ProductId) : ICommand<Result>, IUserRequest;
