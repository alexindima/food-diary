using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Result<ProductResponse>>
{
    private readonly IProductRepository _productRepository;

    public CreateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<ProductResponse>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        // Валидация выполняется автоматически через ValidationBehavior
        var baseUnit = Enum.Parse<Domain.Enums.MeasurementUnit>(command.BaseUnit);
        var visibility = Enum.Parse<Visibility>(command.Visibility);

        var product = Product.Create(
            userId: command.UserId,
            name: command.Name,
            baseUnit: baseUnit,
            baseAmount: command.BaseAmount,
            caloriesPerBase: command.CaloriesPerBase,
            proteinsPerBase: command.ProteinsPerBase,
            fatsPerBase: command.FatsPerBase,
            carbsPerBase: command.CarbsPerBase,
            fiberPerBase: command.FiberPerBase,
            barcode: command.Barcode,
            brand: command.Brand,
            category: command.Category,
            description: command.Description,
            imageUrl: command.ImageUrl,
            visibility: visibility
        );

        product = await _productRepository.AddAsync(product);

        return Result.Success(product.ToResponse());
    }
}
