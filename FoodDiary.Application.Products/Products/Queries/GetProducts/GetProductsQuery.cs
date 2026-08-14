using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Products.Products.Models;

namespace FoodDiary.Application.Products.Products.Queries.GetProducts;

public record GetProductsQuery(
    Guid? UserId,
    int Page,
    int Limit,
    string? Search,
    bool IncludePublic,
    IReadOnlyCollection<string>? ProductTypes = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null)
    : IQuery<Result<PagedResponse<ProductModel>>>, IUserRequest;
