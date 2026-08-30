using FluentValidation;
using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Application.Lessons.Queries.GetLessons;

public sealed class GetLessonsQueryValidator : AbstractValidator<GetLessonsQuery> {
    public GetLessonsQueryValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("User ID is required.");

        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Page).InclusiveBetween(PaginationPolicy.DefaultPage, PaginationPolicy.MaxPageNumber);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationPolicy.MaxPageSize);
    }
}
