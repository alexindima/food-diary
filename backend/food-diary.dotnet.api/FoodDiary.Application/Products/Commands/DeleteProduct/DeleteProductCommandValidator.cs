using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Products.Commands.DeleteProduct;

public class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    private readonly IProductRepository _productRepository;

    public DeleteProductCommandValidator(IProductRepository productRepository)
    {
        _productRepository = productRepository;

        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.ProductId)
            .Must(id => id != ProductId.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("ProductId is required");

        RuleFor(x => x)
            .CustomAsync(async (command, context, cancellationToken) =>
            {
                if (command.UserId is null || command.UserId.Value == UserId.Empty)
                {
                    return;
                }

                var product = await _productRepository.GetByIdAsync(command.ProductId, command.UserId.Value, includePublic: false, cancellationToken: cancellationToken);
                if (product is null)
                {
                    context.AddFailure(new ValidationFailure(nameof(command.ProductId), "Product not found or you do not have permission to delete it")
                    {
                        ErrorCode = "Product.NotFound"
                    });
                }
                else
                {
                    var usageCount = product.MealItems.Count + product.RecipeIngredients.Count;
                    if (usageCount > 0)
                    {
                        context.AddFailure(new ValidationFailure(nameof(command.ProductId), "Product is already used and cannot be deleted")
                        {
                            ErrorCode = "Validation.Invalid"
                        });
                    }
                }
            });
    }
}


