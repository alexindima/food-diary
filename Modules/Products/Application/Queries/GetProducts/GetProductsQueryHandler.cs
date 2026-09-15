using FoodDiary.Modules.Products.Application.Mappings;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Application.Common;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Models;

using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Queries.GetProducts;

public sealed class GetProductsQueryHandler(
    IProductOverviewReadService productOverviewReadService,
    ICurrentUserAccessService currentUserAccessService)
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

        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var response = new PagedResponse<ProductModel>(
            items.Select(product => product.ToModel()).ToList(),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        return Result.Success(response);
    }
}
