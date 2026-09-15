using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Usda.Contracts.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Usda.Application.Commands.UnlinkProductFromUsdaFood;

public sealed class UnlinkProductFromUsdaFoodCommandHandler(
    IUsdaProductLinkService productLinkService,
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
        Result unlinked = await productLinkService.UnlinkAsync(
            productId, userIdResult.Value, cancellationToken).ConfigureAwait(false);

        if (unlinked.IsFailure) {
            return unlinked;
        }

        return Result.Success();
    }
}
