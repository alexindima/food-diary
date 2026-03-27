using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Products.Commands.DeleteProduct;

public record DeleteProductCommand(Guid? UserId, Guid ProductId) : ICommand<Result>;
