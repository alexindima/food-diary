using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Products.Application.Models;

namespace FoodDiary.Modules.Products.Application.Commands.DuplicateProduct;

public sealed record DuplicateProductCommand(
    Guid? UserId,
    Guid ProductId) : ICommand<Result<ProductModel>>, IUserRequest;
