using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetProducts;

public class GetProductsQueryHandler(
    IProductReadRepository productRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetProductsQuery, Result<PagedResponse<ProductModel>>> {
    public async Task<Result<PagedResponse<ProductModel>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<PagedResponse<ProductModel>>(Errors.Authentication.InvalidToken);
        }

        int pageNumber = Math.Max(query.Page, 1);
        int pageSize = Math.Max(query.Limit, 1);
        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<PagedResponse<ProductModel>>(accessError);
        }
        ProductType[]? productTypes = EnumFilterParser.ParseMany<ProductType>(query.ProductTypes);

        (IReadOnlyList<(Domain.Entities.Products.Product Product, int UsageCount)> items, int totalItems) = await productRepository.GetPagedAsync(
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

        var productsWithUsage = items.Select(item => new {
            item.Product,
            item.UsageCount,
            IsOwner = item.Product.UserId == userId,
        }).ToList();

        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var response = new PagedResponse<ProductModel>(
            productsWithUsage.ConvertAll(p => p.Product.ToModel(p.UsageCount, p.IsOwner)),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        return Result.Success(response);
    }
}
