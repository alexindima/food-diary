using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetRecentProducts;

public sealed record GetRecentProductsQuery(UserId? UserId, int Limit, bool IncludePublic)
    : IQuery<Result<IReadOnlyList<ProductResponse>>>;
