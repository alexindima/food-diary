using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Products.Application.Models;

namespace FoodDiary.Modules.Products.Application.Queries.GetProductById;

public record GetProductByIdQuery(Guid? UserId, Guid ProductId) : IQuery<Result<ProductModel>>, IUserRequest;
