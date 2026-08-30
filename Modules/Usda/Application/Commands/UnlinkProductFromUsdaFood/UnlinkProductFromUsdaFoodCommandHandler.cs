using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Usda.Commands.UnlinkProductFromUsdaFood;

public sealed class UnlinkProductFromUsdaFoodCommandHandler(
    IProductUsdaLinkService productLinkService,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<UnlinkProductFromUsdaFoodCommand, Result> {
    public async Task<Result> Handle(
        UnlinkProductFromUsdaFoodCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        var productId = (ProductId)command.ProductId;
        bool unlinked = await productLinkService.UnlinkAsync(
            productId, userIdResult.Value, cancellationToken).ConfigureAwait(false);

        if (!unlinked) {
            return Result.Failure(Errors.Product.NotAccessible(command.ProductId));
        }

        return Result.Success();
    }
}
