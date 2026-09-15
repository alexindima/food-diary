using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Products.Application.Mappings;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Products.Application.Abstractions.Common;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Application.Common;
using FoodDiary.Modules.Users.Contracts.Common;

using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Commands.DuplicateProduct;

public sealed class DuplicateProductCommandHandler(
    IProductReadRepository productReadRepository,
    IProductWriteRepository productWriteRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DuplicateProductCommand, Result<ProductModel>> {
    public async Task<Result<ProductModel>> Handle(DuplicateProductCommand command, CancellationToken cancellationToken) {
        Result<ProductId> productIdResult = ProductRequiredIdParser.Parse(
            command.ProductId,
            nameof(command.ProductId),
            "Product id must not be empty.",
            value => new ProductId(value));
        if (productIdResult.IsFailure) {
            return ProductRequiredIdParser.ToFailure<ProductModel, ProductId>(productIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<ProductModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        ProductId productId = productIdResult.Value;

        Product? original = await productReadRepository.GetByIdAsync(
            productId,
            userId,
            includePublic: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (original is null) {
            return Result.Failure<ProductModel>(ProductErrors.NotAccessible(command.ProductId));
        }

        bool isOwnedByCurrentUser = original.UserId == userId;
        var duplicate = Product.Create(
            userId,
            original.Name,
            original.BaseUnit,
            original.BaseAmount,
            original.DefaultPortionAmount,
            original.CaloriesPerBase,
            original.ProteinsPerBase,
            original.FatsPerBase,
            original.CarbsPerBase,
            original.FiberPerBase,
            original.AlcoholPerBase,
            original.Barcode,
            original.Brand,
            original.ProductType,
            original.Category,
            original.Description,
            isOwnedByCurrentUser ? original.Comment : null,
            original.ImageUrl,
            imageAssetId: null,
            original.Visibility);

        await productWriteRepository.AddAsync(duplicate, cancellationToken).ConfigureAwait(false);

        return Result.Success(duplicate.ToModel(isOwnedByCurrentUser: true));
    }
}
