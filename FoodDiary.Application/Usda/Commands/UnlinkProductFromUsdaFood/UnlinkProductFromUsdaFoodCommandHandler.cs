using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Products;

namespace FoodDiary.Application.Usda.Commands.UnlinkProductFromUsdaFood;

public class UnlinkProductFromUsdaFoodCommandHandler(IProductRepository productRepository)
    : ICommandHandler<UnlinkProductFromUsdaFoodCommand, Result> {
    public async Task<Result> Handle(
        UnlinkProductFromUsdaFoodCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        var productId = (ProductId)command.ProductId;
        Product? product = await productRepository.GetByIdForUpdateAsync(
            productId, userIdResult.Value, includePublic: false, cancellationToken).ConfigureAwait(false);

        if (product is null) {
            return Result.Failure(Errors.Product.NotAccessible(command.ProductId));
        }

        product.UnlinkUsdaFood();
        await productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
