using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Usda.Application.Abstractions.Common;
using FoodDiary.Modules.Usda.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Usda.Domain.Entities;

namespace FoodDiary.Modules.Usda.Application.Commands.LinkProductToUsdaFood;

public sealed class LinkProductToUsdaFoodCommandHandler(
    IUsdaProductLinkService productLinkService,
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
        Result isAccessible = await productLinkService.IsAccessibleForUpdateAsync(
            productId, userIdResult.Value, cancellationToken).ConfigureAwait(false);

        if (isAccessible.IsFailure) {
            return isAccessible;
        }

        UsdaFood? usdaFood = await usdaFoodRepository.GetByFdcIdAsync(command.FdcId, cancellationToken).ConfigureAwait(false);
        if (usdaFood is null) {
            return Result.Failure(UsdaErrors.FoodNotFound(command.FdcId));
        }

        Result linked = await productLinkService.LinkAsync(
            productId,
            userIdResult.Value,
            command.FdcId,
            cancellationToken).ConfigureAwait(false);
        if (linked.IsFailure) {
            return linked;
        }

        return Result.Success();
    }
}
