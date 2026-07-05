using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler(
    IProductOverviewReadService productOverviewReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetProductByIdQuery, Result<ProductModel>> {
    public async Task<Result<ProductModel>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<ProductModel>(Errors.Authentication.InvalidToken);
        }

        if (query.ProductId == Guid.Empty) {
            return Result.Failure<ProductModel>(Errors.Validation.Invalid(nameof(query.ProductId), "Product id must not be empty."));
        }

        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<ProductModel>(accessError);
        }
        var productId = new ProductId(query.ProductId);
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
