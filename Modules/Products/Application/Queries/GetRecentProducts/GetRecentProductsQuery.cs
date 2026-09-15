using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Products.Application.Models;

namespace FoodDiary.Modules.Products.Application.Queries.GetRecentProducts;

public sealed record GetRecentProductsQuery(Guid? UserId, int Limit, bool IncludePublic)
    : IQuery<Result<IReadOnlyList<ProductModel>>>, IUserRequest;
