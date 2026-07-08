using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.Queries.GetProducts;

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
