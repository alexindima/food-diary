using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.Commands.DuplicateProduct;

public sealed record DuplicateProductCommand(
    Guid? UserId,
    Guid ProductId) : ICommand<Result<ProductModel>>, IUserRequest;
