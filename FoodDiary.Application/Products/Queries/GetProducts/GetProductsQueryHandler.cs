using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetProducts;

public class GetProductsQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductsQuery, Result<PagedResponse<ProductModel>>> {
    public async Task<Result<PagedResponse<ProductModel>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<PagedResponse<ProductModel>>(Errors.Authentication.InvalidToken);
        }

        var pageNumber = Math.Max(query.Page, 1);
        var pageSize = Math.Max(query.Limit, 1);
        var userId = new UserId(query.UserId.Value);
        var productTypes = query.ProductTypes?
            .Select(type => Enum.TryParse<ProductType>(type, true, out var parsed) ? parsed : (ProductType?)null)
            .OfType<ProductType>()
            .Distinct()
            .ToArray();

        var (items, totalItems) = await productRepository.GetPagedAsync(
            userId,
            query.IncludePublic,
            pageNumber,
            pageSize,
            query.Search,
            productTypes is { Length: > 0 } ? productTypes : null,
            cancellationToken);

        var productsWithUsage = items.Select(item => new {
            item.Product,
            item.UsageCount,
            IsOwner = item.Product.UserId == userId
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var response = new PagedResponse<ProductModel>(
            productsWithUsage.Select(p => p.Product.ToModel(p.UsageCount, p.IsOwner)).ToList(),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        return Result.Success(response);
    }
}
