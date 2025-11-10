using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Products.Commands.DuplicateProduct;

public sealed record DuplicateProductCommand(
    UserId? UserId,
    ProductId ProductId) : ICommand<Result<ProductResponse>>;
