using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.DeleteProduct;

public class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand> {
    public DeleteProductCommandValidator(IProductRepository productRepository) {

        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.ProductId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("ProductId is required");

        RuleFor(x => x)
            .CustomAsync(async (command, context, cancellationToken) => {
                if (command.UserId is null || command.UserId.Value == Guid.Empty) {
                    return;
                }

                var product = await productRepository.GetByIdAsync(new ProductId(command.ProductId), new UserId(command.UserId.Value), includePublic: false, cancellationToken: cancellationToken);
                if (product is null) {
                    context.AddFailure(new ValidationFailure(nameof(command.ProductId), "Product not found or you do not have permission to delete it") {
                        ErrorCode = "Product.NotFound"
                    });
                } else {
                    var usageCount = product.MealItems.Count + product.RecipeIngredients.Count;
                    if (usageCount > 0) {
                        context.AddFailure(new ValidationFailure(nameof(command.ProductId), "Product is already used and cannot be deleted") {
                            ErrorCode = "Validation.Invalid"
                        });
                    }
                }
            });
    }
}
