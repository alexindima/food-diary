using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Contracts.Products;

namespace FoodDiary.Application.Products.Queries.GetProducts;

public class GetProductsQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductsQuery, Result<IEnumerable<ProductResponse>>>
{
    public async Task<Result<IEnumerable<ProductResponse>>>
        Handle(GetProductsQuery query, CancellationToken cancellationToken)
    {
        // Получаем продукты с динамическим подсчётом UsageCount
        var products = await productRepository.GetAllAsync(query.UserId);

        // Вычисляем UsageCount для каждого продукта
        var productsWithUsage = products.Select(product => new
        {
            Product = product,
            UsageCount = product.MealItems.Count + product.RecipeIngredients.Count
        }).ToList();

        return Result.Success(productsWithUsage.Select(p => p.Product.ToResponse(p.UsageCount)));
    }
}