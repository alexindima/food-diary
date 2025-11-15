using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(
    UserId? UserId,
    string? Barcode,
    string Name,
    string? Brand,
    string ProductType,
    string? Category,
    string? Description,
    string? Comment,
    string? ImageUrl,
    string BaseUnit,
    double BaseAmount,
    double CaloriesPerBase,
    double ProteinsPerBase,
    double FatsPerBase,
    double CarbsPerBase,
    double FiberPerBase,
    string Visibility
) : ICommand<Result<ProductResponse>>;
