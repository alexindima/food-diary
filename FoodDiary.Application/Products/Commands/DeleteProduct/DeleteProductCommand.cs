using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Products.Commands.DeleteProduct;

public record DeleteProductCommand(Guid? UserId, Guid ProductId) : ICommand<Result>, IUserRequest;
