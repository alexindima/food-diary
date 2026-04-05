using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Usda.Commands.LinkProductToUsdaFood;

public record LinkProductToUsdaFoodCommand(
    Guid? UserId,
    Guid ProductId,
    int FdcId) : ICommand<Result>, IUserRequest;
