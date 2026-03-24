using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.DeleteProduct;

public record DeleteProductCommand(Guid? UserId, ProductId ProductId) : ICommand<Result<bool>>;
