using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Products.Products.Models;

namespace FoodDiary.Application.Products.Products.Queries.GetRecentProducts;

public sealed record GetRecentProductsQuery(Guid? UserId, int Limit, bool IncludePublic)
    : IQuery<Result<IReadOnlyList<ProductModel>>>, IUserRequest;
