using FluentValidation;

namespace FoodDiary.Application.Lessons.Queries.GetLessons;

public class GetLessonsQueryValidator : AbstractValidator<GetLessonsQuery> {
    public GetLessonsQueryValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("User ID is required.");
    }
}
