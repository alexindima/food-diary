using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Products.Products.Models;

namespace FoodDiary.Application.Products.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid? UserId, Guid ProductId) : IQuery<Result<ProductModel>>, IUserRequest;
