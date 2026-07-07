using FoodDiary.Application.Abstractions.Products.Common;
using FluentValidation;
using FluentValidation.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
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
                Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
                Result<ProductId> productIdResult = RequiredIdParser.Parse(
                    command.ProductId,
                    nameof(command.ProductId),
                    "Product id must not be empty.",
                    value => new ProductId(value));
                if (userIdResult.IsFailure || productIdResult.IsFailure) {
                    return;
                }

                Product? product = await productRepository.GetByIdAsync(
                    productIdResult.Value,
                    userIdResult.Value,
                    includePublic: false,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                if (product is null) {
                    context.AddFailure(new ValidationFailure(nameof(command.ProductId), "Product not found or you do not have permission to delete it") {
                        ErrorCode = "Product.NotFound",
                    });
                }
            });
    }
}
