using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Usda.Application.Commands.LinkProductToUsdaFood;

public record LinkProductToUsdaFoodCommand(
    Guid? UserId,
    Guid ProductId,
    int FdcId) : ICommand<Result>, IUserRequest;
