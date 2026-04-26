using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Products.Commands.DeleteProduct;

public record DeleteProductCommand(Guid? UserId, Guid ProductId) : ICommand<Result>, IUserRequest;
