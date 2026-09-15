using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Products.Application.Commands.DeleteProduct;

public record DeleteProductCommand(Guid? UserId, Guid ProductId) : ICommand<Result>, IUserRequest;
