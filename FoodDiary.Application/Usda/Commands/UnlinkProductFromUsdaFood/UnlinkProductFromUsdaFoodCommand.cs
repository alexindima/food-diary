using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Usda.Commands.UnlinkProductFromUsdaFood;

public record UnlinkProductFromUsdaFoodCommand(
    Guid? UserId,
    Guid ProductId) : ICommand<Result>, IUserRequest;
