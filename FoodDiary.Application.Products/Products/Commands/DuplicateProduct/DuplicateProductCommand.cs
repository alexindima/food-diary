using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Products.Products.Models;

namespace FoodDiary.Application.Products.Products.Commands.DuplicateProduct;

public sealed record DuplicateProductCommand(
    Guid? UserId,
    Guid ProductId) : ICommand<Result<ProductModel>>, IUserRequest;
