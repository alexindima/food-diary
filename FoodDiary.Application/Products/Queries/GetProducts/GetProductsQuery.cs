using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Common;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetProducts;

public record GetProductsQuery(
    UserId? UserId,
    int Page,
    int Limit,
    string? Search,
    bool IncludePublic,
    IReadOnlyCollection<string>? ProductTypes = null)
    : IQuery<Result<PagedResponse<ProductResponse>>>;
