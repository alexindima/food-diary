using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid? UserId, Guid ProductId) : IQuery<Result<ProductModel>>, IUserRequest;
