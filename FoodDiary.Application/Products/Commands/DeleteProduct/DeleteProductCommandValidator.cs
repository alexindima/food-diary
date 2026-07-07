using FoodDiary.Application.Abstractions.Products.Common;
using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand> {
    public DeleteProductCommandValidator(IProductReadRepository productRepository) {

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
                if (command.UserId is null || command.UserId.Value == Guid.Empty || command.ProductId == Guid.Empty) {
                    return;
                }

                var productId = new ProductId(command.ProductId);
                var userId = new UserId(command.UserId.Value);
                Product? product = await productRepository.GetByIdAsync(productId, userId, includePublic: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (product is null) {
                    context.AddFailure(new ValidationFailure(nameof(command.ProductId), "Product not found or you do not have permission to delete it") {
                        ErrorCode = "Product.NotFound",
                    });
                } else {
                    int usageCount = await productRepository.GetUsageCountAsync(
                        product.Id,
                        product.UserId,
                        includePublic: false,
                        cancellationToken).ConfigureAwait(false);
                    if (usageCount > 0) {
                        context.AddFailure(new ValidationFailure(nameof(command.ProductId), "Product is already used and cannot be deleted") {
                            ErrorCode = "Validation.Invalid",
                        });
                    }
                }
            });
    }
}
