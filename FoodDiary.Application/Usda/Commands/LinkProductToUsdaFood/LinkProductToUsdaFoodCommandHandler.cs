using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Usda.Commands.LinkProductToUsdaFood;

public class LinkProductToUsdaFoodCommandHandler(
    IProductRepository productRepository,
    IUsdaFoodRepository usdaFoodRepository)
    : ICommandHandler<LinkProductToUsdaFoodCommand, Result> {
    public async Task<Result> Handle(
        LinkProductToUsdaFoodCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        var productId = (ProductId)command.ProductId;
        var product = await productRepository.GetByIdForUpdateAsync(
            productId, userIdResult.Value, includePublic: false, cancellationToken);

        if (product is null) {
            return Result.Failure(Errors.Product.NotAccessible(command.ProductId));
        }

        var usdaFood = await usdaFoodRepository.GetByFdcIdAsync(command.FdcId, cancellationToken);
        if (usdaFood is null) {
            return Result.Failure(Errors.Usda.FoodNotFound(command.FdcId));
        }

        product.LinkToUsdaFood(command.FdcId);
        await productRepository.UpdateAsync(product, cancellationToken);

        return Result.Success();
    }
}
