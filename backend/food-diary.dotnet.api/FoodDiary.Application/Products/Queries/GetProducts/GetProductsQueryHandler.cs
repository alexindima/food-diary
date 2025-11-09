using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Contracts.Common;
using FoodDiary.Contracts.Products;

namespace FoodDiary.Application.Products.Queries.GetProducts;

public class GetProductsQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductsQuery, Result<PagedResponse<ProductResponse>>>
{
    public async Task<Result<PagedResponse<ProductResponse>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(query.Page, 1);
        var pageSize = Math.Max(query.Limit, 1);

        var (items, totalItems) = await productRepository.GetPagedAsync(
            query.UserId,
            pageNumber,
            pageSize,
            query.Search,
            cancellationToken);

        var productsWithUsage = items.Select(product => new
        {
            Product = product,
            UsageCount = product.MealItems.Count + product.RecipeIngredients.Count
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var response = new PagedResponse<ProductResponse>(
            productsWithUsage.Select(p => p.Product.ToResponse(p.UsageCount)).ToList(),
            pageNumber,
            pageSize,
            totalPages,
            totalItems);

        return Result.Success(response);
    }
}
