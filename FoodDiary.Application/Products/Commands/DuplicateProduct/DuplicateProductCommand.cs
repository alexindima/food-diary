using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Products.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.DuplicateProduct;

public sealed record DuplicateProductCommand(
    UserId? UserId,
    ProductId ProductId) : ICommand<Result<ProductModel>>;
