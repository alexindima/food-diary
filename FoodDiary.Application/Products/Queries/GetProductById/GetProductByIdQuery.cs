using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Products.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid? UserId, ProductId ProductId) : IQuery<Result<ProductModel>>;
