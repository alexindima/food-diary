using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.Queries.GetRecentProducts;

public sealed record GetRecentProductsQuery(Guid? UserId, int Limit, bool IncludePublic)
    : IQuery<Result<IReadOnlyList<ProductModel>>>, IUserRequest;
