using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Products.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetRecentProducts;

public sealed record GetRecentProductsQuery(UserId? UserId, int Limit, bool IncludePublic)
    : IQuery<Result<IReadOnlyList<ProductModel>>>;
