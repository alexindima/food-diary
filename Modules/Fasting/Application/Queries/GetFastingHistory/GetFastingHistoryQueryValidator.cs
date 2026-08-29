using FluentValidation;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Modules.Fasting.Application.Queries.GetFastingHistory;

public sealed class GetFastingHistoryQueryValidator : AbstractValidator<GetFastingHistoryQuery> {
    public GetFastingHistoryQueryValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.Page)
            .InclusiveBetween(PaginationPolicy.DefaultPage, PaginationPolicy.MaxPageNumber)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Page must be between {PaginationPolicy.DefaultPage} and {PaginationPolicy.MaxPageNumber}.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Limit must be between 1 and 50.");

        RuleFor(x => x)
            .Must(x => x.From <= x.To)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("From must be earlier than or equal to To.");
    }
}
