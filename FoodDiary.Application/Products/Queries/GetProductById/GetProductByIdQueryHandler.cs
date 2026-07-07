using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler(
    IProductOverviewReadService productOverviewReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetProductByIdQuery, Result<ProductModel>> {
    public async Task<Result<ProductModel>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken) {
        Result<ProductId> productIdResult = RequiredIdParser.Parse(
            query.ProductId,
            nameof(query.ProductId),
            "Product id must not be empty.",
            value => new ProductId(value));
        if (productIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<ProductModel, ProductId>(productIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ProductModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        ProductId productId = productIdResult.Value;
        IReadOnlyDictionary<ProductId, ProductOverviewReadItem> productsById = await productOverviewReadService.GetByIdsWithUsageAsync(
            [productId],
            userId,
            includePublic: false,
            cancellationToken).ConfigureAwait(false);
        ProductOverviewReadItem? product = productsById.GetValueOrDefault(productId);
        if (product is null) {
            return Result.Failure<ProductModel>(Errors.Product.NotAccessible(query.ProductId));
        }

        return Result.Success(product.ToModel());
    }
}
