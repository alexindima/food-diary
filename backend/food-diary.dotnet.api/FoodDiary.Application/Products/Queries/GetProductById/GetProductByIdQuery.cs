using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery(UserId? UserId, ProductId ProductId) : IQuery<Result<ProductResponse>>;
