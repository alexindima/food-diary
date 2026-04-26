using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.Queries.GetProducts;

public record GetProductsQuery(
    Guid? UserId,
    int Page,
    int Limit,
    string? Search,
    bool IncludePublic,
    IReadOnlyCollection<string>? ProductTypes = null)
    : IQuery<Result<PagedResponse<ProductModel>>>, IUserRequest;
