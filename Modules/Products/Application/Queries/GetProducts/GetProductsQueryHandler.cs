using FoodDiary.Modules.Products.Application.Mappings;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Application.Common;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Application.Contracts.Common.Models;

using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadFavoriteProductOverview;

namespace FoodDiary.Modules.Products.Application.Queries.GetProducts;

public sealed class GetProductsQueryHandler(
    IProductOverviewReadService productOverviewReadService,
    ICurrentUserAccessService currentUserAccessService,
    ISender sender)
    : IQueryHandler<GetProductsQuery, Result<PagedResponse<ProductModel>>> {
    public async Task<Result<PagedResponse<ProductModel>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken) {
        int pageNumber = PaginationPolicy.NormalizePage(query.Page);
        int pageSize = PaginationPolicy.NormalizePageSize(query.Limit, defaultPageSize: 1);
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<PagedResponse<ProductModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        ProductType[]? productTypes = ProductEnumFilterParser.ParseMany<ProductType>(query.ProductTypes);

        (IReadOnlyList<ProductOverviewReadItem> items, int totalItems) = await productOverviewReadService.GetPagedAsync(
            userId,
            query.IncludePublic,
            pageNumber,
            pageSize,
            new ProductQueryFilters(
                query.Search,
                productTypes,
                query.CaloriesFrom,
                query.CaloriesTo,
                query.HasImage),
            cancellationToken).ConfigureAwait(false);

        FavoriteProductOverviewModel favorites = items.Count == 0
            ? new([], [], 0)
            : await sender.Send(new ReadFavoriteProductOverviewQuery(userId, [.. items.Select(product => product.Id)]), cancellationToken).ConfigureAwait(false);
        var favoritesByProductId = favorites.Items.ToDictionary(favorite => favorite.ProductId);
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var response = new PagedResponse<ProductModel>(
            items.Select(product => {
                FavoriteProductModel? favorite = favoritesByProductId.GetValueOrDefault(product.Id.Value);
                return product.ToModel(favorite is not null, favorite?.Id);
            }).ToList(),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        return Result.Success(response);
    }
}
