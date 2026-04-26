using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Usda.Commands.UnlinkProductFromUsdaFood;

public class UnlinkProductFromUsdaFoodCommandHandler(IProductRepository productRepository)
    : ICommandHandler<UnlinkProductFromUsdaFoodCommand, Result> {
    public async Task<Result> Handle(
        UnlinkProductFromUsdaFoodCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        var productId = (ProductId)command.ProductId;
        var product = await productRepository.GetByIdAsync(
            productId, userIdResult.Value, includePublic: false, cancellationToken);

        if (product is null) {
            return Result.Failure(Errors.Product.NotAccessible(command.ProductId));
        }

        product.UnlinkUsdaFood();
        await productRepository.UpdateAsync(product, cancellationToken);

        return Result.Success();
    }
}
