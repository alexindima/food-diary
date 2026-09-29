using FoodDiary.Application.Contracts.Common.Validation;
using FluentValidation;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetRecommendationComments;

public sealed class GetRecommendationCommentsQueryValidator : AbstractValidator<GetRecommendationCommentsQuery> {
    public GetRecommendationCommentsQueryValidator() {
        RuleFor(query => query.Page).InclusiveBetween(1, PaginationPolicy.MaxPageNumber).WithErrorCode("Validation.Invalid");
        RuleFor(query => query.Limit).InclusiveBetween(1, PaginationPolicy.MaxPageSize).WithErrorCode("Validation.Invalid");
    }
}
