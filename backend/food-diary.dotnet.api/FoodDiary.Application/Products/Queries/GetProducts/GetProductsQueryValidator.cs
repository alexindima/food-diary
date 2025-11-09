using FluentValidation;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Products.Queries.GetProducts;

public class GetProductsQueryValidator : AbstractValidator<GetProductsQuery> {
    public GetProductsQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Не удалось определить пользователя")
            .Must(userId => userId is not null && userId.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Не удалось определить пользователя");

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("page must be greater than 0");

        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("limit must be greater than 0");
    }
}