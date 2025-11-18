using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Products.Commands.DeleteProduct;

public record DeleteProductCommand(UserId? UserId, ProductId ProductId) : ICommand<Result<bool>>;
