using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Products;

namespace FoodDiary.Application.Usda.Commands.UnlinkProductFromUsdaFood;

public sealed class UnlinkProductFromUsdaFoodCommandHandler(IUsdaProductLinkWriteRepository productLinkRepository)
    : ICommandHandler<UnlinkProductFromUsdaFoodCommand, Result> {
    public async Task<Result> Handle(
        UnlinkProductFromUsdaFoodCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        var productId = (ProductId)command.ProductId;
        Product? product = await productLinkRepository.GetForLinkUpdateAsync(
            productId, userIdResult.Value, cancellationToken).ConfigureAwait(false);

        if (product is null) {
            return Result.Failure(Errors.Product.NotAccessible(command.ProductId));
        }

        product.UnlinkUsdaFood();
        await productLinkRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
