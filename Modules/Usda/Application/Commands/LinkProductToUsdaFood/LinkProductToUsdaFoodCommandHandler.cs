using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Usda;

namespace FoodDiary.Application.Usda.Commands.LinkProductToUsdaFood;

public sealed class LinkProductToUsdaFoodCommandHandler(
    IProductUsdaLinkService productLinkService,
    IUsdaFoodReadRepository usdaFoodRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<LinkProductToUsdaFoodCommand, Result> {
    public async Task<Result> Handle(
        LinkProductToUsdaFoodCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        var productId = (ProductId)command.ProductId;
        bool isAccessible = await productLinkService.IsAccessibleForUpdateAsync(
            productId, userIdResult.Value, cancellationToken).ConfigureAwait(false);

        if (!isAccessible) {
            return Result.Failure(Errors.Product.NotAccessible(command.ProductId));
        }

        UsdaFood? usdaFood = await usdaFoodRepository.GetByFdcIdAsync(command.FdcId, cancellationToken).ConfigureAwait(false);
        if (usdaFood is null) {
            return Result.Failure(Errors.Usda.FoodNotFound(command.FdcId));
        }

        bool linked = await productLinkService.LinkAsync(
            productId,
            userIdResult.Value,
            command.FdcId,
            cancellationToken).ConfigureAwait(false);
        if (!linked) {
            return Result.Failure(Errors.Product.NotAccessible(command.ProductId));
        }

        return Result.Success();
    }
}
